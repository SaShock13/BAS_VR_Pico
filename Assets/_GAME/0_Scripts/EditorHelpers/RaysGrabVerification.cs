#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public static class RaysGrabVerification
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/BAS VR/Verify Rays Root and Detail Grab")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        var configs = AssetDatabase.FindAssets("t:PartConfig").Select(g => AssetDatabase.LoadAssetAtPath<PartConfig>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        int frames = 0;
        foreach (var config in configs.Where(c => c.Prefab != null))
        {
            if (config.PartType == PartType.Rays)
            {
                var envelope = config.Prefab.GetComponent<DroneGrabEnvelope>();
                Require(envelope != null && envelope.MainCollider != null, "Missing frame envelope: " + config.name);
                Require(config.Prefab.GetComponent<XRGrabInteractable>().colliders.Contains(envelope.MainCollider), "Unregistered frame collider");
                Require(config.Prefab.GetComponentsInChildren<SocketView>(true).Any(s => s.AllowedTypes.Contains(PartType.Body)), "Missing body mount");
                Require(config.Prefab.GetComponent<DronePhysicsSimulation>() != null, "Missing frame flight simulation");
                frames++;
            }
            if (config.PartType == PartType.Body)
            {
                Require(!config.Prefab.GetComponentsInChildren<SocketView>(true).Any(s => s.AllowedTypes.Contains(PartType.Rays)), "Body still accepts root rays");
                Require(config.Prefab.GetComponent<DroneView>() == null && config.Prefab.GetComponent<DronePhysicsSimulation>() == null,
                    "Body still owns whole-drone input or simulation");
            }
        }
        var logger = new TestLogger();
        using (var bus = new EventBus(logger, new DiContainer()))
        {
            bus.Initialize();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var registry = new PartViewRegistry(bus);
                var assembly = new Clean_AssemblySystem(bus, logger, new PartConfigRepository(configs), null, registry,
                    new DiContainer(), null, null, null, null);
                typeof(Clean_AssemblySystem).GetField("_userActivity", Hidden).SetValue(assembly, new TestActivity());
                var domains = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts", Hidden).GetValue(assembly);
                var views = new Dictionary<string, DronePartView>();
                var frameConfig = configs.Single(c => c.PartId == "newparts/Rays/DRONE1_Rays");
                var bodyConfig = configs.Single(c => c.PartId == "newparts/Bodies/DRONE1_Body");
                var motorConfig = configs.First(c => c.PartType == PartType.Motor && c.Prefab != null && c.PartId.StartsWith("newparts/"));
                var propConfig = configs.First(c => c.PartType == PartType.Propeller && c.Prefab != null && c.PartId.StartsWith("newparts/"));
                var frame = Create(frameConfig, "frame", scene, bus, registry, domains, views);
                var body = Create(bodyConfig, "body", scene, bus, registry, domains, views);
                var motor = Create(motorConfig, "motor", scene, bus, registry, domains, views);
                var prop = Create(propConfig, "prop", scene, bus, registry, domains, views);
                var loose = Create(motorConfig, "loose", scene, bus, registry, domains, views);
                Attach(body, frame, domains, PartType.Body);
                Attach(motor, frame, domains, PartType.Motor);
                Attach(prop, motor, domains, PartType.Propeller);
                frame.transform.SetPositionAndRotation(new Vector3(2, 1, 3), Quaternion.Euler(0, 32, 0));
                loose.transform.position = new Vector3(20, 2, 10);
                assembly.RebuildDrones();
                Require(assembly.TryGetDroneForPart("prop", out var drone) && drone.InstanceId == "frame", "Rays is not the drone root");
                using (var mode = new AssemblyGrabModeService(bus, assembly, registry))
                using (var detach = bus.Subscribe<PartSocketDetachRequest>(e => typeof(Clean_AssemblySystem).GetMethod("OnDetachRequested", Hidden).Invoke(assembly, new object[] { e })))
                {
                    mode.Initialize();
                    Require(frame.GrabEnvelope.MainCollider.enabled, "Default whole-drone collider disabled");
                    Require(!body.Process((IXRHoverInteractor)null, null) && !motor.Process((IXRHoverInteractor)null, null), "Installed parts intercept the root");
                    Require(loose.Process((IXRHoverInteractor)null, null), "Loose parts are blocked");
                    var handObject = new GameObject("Verification hand");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(handObject, scene);
                    var hand = handObject.AddComponent<XRRayInteractor>();
                    var rootGrab = frame.GetComponent<XRGrabInteractable>();
                    var rootHands = (IList<IXRSelectInteractor>)rootGrab.interactorsSelecting;
                    var handSelection = (IList<IXRSelectInteractable>)hand.interactablesSelected;
                    rootHands.Add(hand);
                    handSelection.Add(rootGrab);
                    // XRIT caches isSelected separately from the list; mirror both for this Edit Mode fixture.
                    typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(rootGrab, true);
                    bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = true });
                    Require(frame.GrabEnvelope.MainCollider.enabled && !body.Process((IXRHoverInteractor)null, null), "Mode switch interrupted held drone");
                    rootHands.Clear();
                    handSelection.Clear();
                    typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(rootGrab, false);
                    bus.Publish(new PartTransformChangedEvent { instanceId = "frame" });
                    Require(!frame.GrabEnvelope.MainCollider.enabled, "Deferred switch not applied after release");
                    bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = false });
                    Physics.SyncTransforms();
                    foreach (var view in new[] { frame, body, motor, prop })
                    {
                        var bounds = new Bounds(); bool found = false;
                        view.EncapsulateOwnGeometry(frame.transform, ref bounds, ref found);
                        if (found) Require(frame.GrabEnvelope.MainCollider.bounds.Contains(frame.transform.TransformPoint(bounds.center)), "Frame bounds miss an installed part");
                    }
                    bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = true });
                    Require(!frame.GrabEnvelope.MainCollider.enabled && motor.Process((IXRHoverInteractor)null, null), "Held detail mode failed");
                    bus.Publish(new PartGrabStartedEvent { InstanceId = "motor" });
                    Require(domains["motor"].AttachedPartInstanceId == null && motor.transform.parent == null, "Grabbing detail did not detach it");
                    Require(domains["prop"].AttachedPartInstanceId == "motor" && prop.transform.IsChildOf(motor.transform), "Descendants detached independently");
                    bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = false });
                    Require(frame.GrabEnvelope.MainCollider.enabled && !body.Process((IXRHoverInteractor)null, null), "Release did not restore whole-drone mode");
                    Require(motor.Process((IXRHoverInteractor)null, null), "Detached subassembly became blocked");
                    Require(!assembly.CanAttach("frame", body.GetSocket("mount/Rays/4")), "Rays can still be installed under body");
                    var saved = assembly.BuildSaveData();
                    Require(saved.Version == 2 && saved.Parts.Single(p => p.InstanceId == "frame").AttachedPartId == null, "New save has wrong root");

                    // Simulate a version-1 graph, then invoke the same reroot path used after loading.
                    body.Detach(); domains["body"].Detach();
                    var oldMount = body.GetSocket("mount/Rays/4");
                    frame.AttachTo(oldMount.transform);
                    frame.transform.localPosition = new Vector3(0.02f, 0.01f, -0.03f);
                    domains["frame"].AttachToPartSocket("body", oldMount.SocketId);
                    var legacySave = assembly.BuildSaveData();
                    legacySave.Version = 1;
                    var garageData = new GarageSaveData();
                    garageData.Drones.Add(new GarageDroneData { DroneId = "body", GarageId = "garage", Assembly = legacySave,
                        metaData = new DroneMetadata { DroneId = "body", Name = "Saved drone" } });
                    var garage = new GarageService(assembly, new TestGarageSave { Data = garageData });
                    Require(garage.GetByDroneId("frame")?.metaData.Name == "Saved drone", "Legacy garage identity or name lost");
                    var poses = views.ToDictionary(p => p.Key, p => (p.Value.transform.position, p.Value.transform.rotation, p.Value.transform.lossyScale));
                    typeof(Clean_AssemblySystem).GetMethod("RerootLegacyAssemblies", Hidden).Invoke(assembly, new object[] { domains, views });
                    Require(domains["frame"].AttachedPartInstanceId == null && domains["body"].AttachedPartInstanceId == "frame", "Legacy root not reversed");
                    foreach (var pair in views)
                    {
                        var pose = poses[pair.Key];
                        Require(Vector3.Distance(pair.Value.transform.position, pose.position) < 0.001f &&
                            Quaternion.Angle(pair.Value.transform.rotation, pose.rotation) < 0.1f &&
                            Vector3.Distance(pair.Value.transform.lossyScale, pose.lossyScale) < 0.001f, "Migration moved or resized: " + pair.Key);
                    }
                    assembly.RebuildDrones();
                    var roundTrip = JsonUtility.FromJson<AssemblySaveData>(JsonUtility.ToJson(assembly.BuildSaveData()));
                    foreach (var row in roundTrip.Parts)
                    {
                        var restored = PartMapper.ToDomain(row);
                        Require(restored.AttachedPartInstanceId == domains[row.InstanceId].AttachedPartInstanceId &&
                            restored.AttachedSocketId == domains[row.InstanceId].AttachedSocketId, "New save graph roundtrip failed");
                    }
                    mode.Refresh();
                    var envelope = frame.GrabEnvelope;
                    var originalCollider = frameConfig.Prefab.GetComponent<DroneGrabEnvelope>().MainCollider;
                    envelope.PrepareForFlight();
                    Require(envelope.MainCollider.enabled && envelope.MainCollider.center == originalCollider.center &&
                        envelope.MainCollider.size == originalCollider.size, "Flight did not restore the real frame collider");
                }
                Require(logger.Errors == 0, "Event handler errors occurred");
                Require(bus.GetSubscriberCount<SetAssemblyDetailModeRequest>() == 0, "Mode subscriptions leaked");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var socket in root.GetComponentsInChildren<SocketView>(true))
                        typeof(SocketView).GetMethod("OnDestroy", Hidden).Invoke(socket, null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        Debug.Log($"PASS: {frames} Rays prefabs; Body mounts; root membership; whole/detail/loose grabs; subtree detach; release; new saves; legacy world-pose/scale preservation; flight collider; subscriptions. Edit Mode only.");
    }

    private static DronePartView Create(PartConfig config, string id, UnityEngine.SceneManagement.Scene scene, IEventBus bus,
        PartViewRegistry registry, Dictionary<string, PartDomainState> domains, Dictionary<string, DronePartView> views)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab, scene);
        var view = go.AddComponent<DronePartView>();
        typeof(DronePartView).GetMethod("Awake", Hidden).Invoke(view, null);
        view.Init(id, bus);
        registry.Register(id, go);
        domains.Add(id, new PartDomainState(id, config.PartId, config.PartType, new PartVisualProperties()) { isLoaded = true });
        views.Add(id, view);
        return view;
    }

    private static void Attach(DronePartView child, DronePartView parent, Dictionary<string, PartDomainState> domains, PartType type)
    {
        var socket = parent.GetSocketByType(type);
        Require(socket != null, "Missing socket for " + type);
        child.AttachTo(socket.transform);
        child.transform.localPosition = Vector3.zero;
        domains[child.InstanceId].AttachToPartSocket(parent.InstanceId, socket.SocketId);
    }

    private static void Require(bool valid, string message) { if (!valid) throw new Exception(message); }
    private sealed class TestActivity : IUserActivityService { public float IdleTime => 0; public void NotifyActivity() { } }
    private sealed class TestGarageSave : IGarageSaveService
    {
        public GarageSaveData Data;
        public GarageSaveData Load() => Data;
        public void Save(GarageSaveData data) => Data = data;
    }
    private sealed class TestLogger : IAppLogger
    {
        public int Errors;
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { Errors++; Debug.LogWarning(message); }
    }
}
#endif
