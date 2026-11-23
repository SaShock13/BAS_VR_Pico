#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

// Integration checks against real part prefabs and XRIT's collider lookup, without entering Play Mode.
public static class BottomPlateGrabVerification
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/BAS VR/Verify BottomPlate Grab Routing")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode only.");
        var configs = AssetDatabase.FindAssets("t:PartConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<PartConfig>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var plates = configs.Where(c => c.PartType == PartType.BottomPlate && c.Prefab != null)
            .GroupBy(c => c.Prefab).Select(g => g.First()).ToArray();
        Require(plates.Length >= 2, "Expected both plate prefabs.");
        foreach (var plate in plates) Verify(plate, configs);
        Debug.Log("PASS: BottomPlate routing on R1/R2; real XRIT collider lookup; whole/detail/loose/subtree ownership; held-grab stability; both-hand highlight; disposal.");
    }

    private static void Verify(PartConfig plateConfig, PartConfig[] configs)
    {
        var logger = new Logger();
        using (var bus = new EventBus(logger, new DiContainer()))
        {
            bus.Initialize();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var registry = new PartViewRegistry(bus);
                var assembly = new Clean_AssemblySystem(bus, logger, new PartConfigRepository(configs), null,
                    registry, new DiContainer(), null, null, null, null);
                typeof(Clean_AssemblySystem).GetField("_userActivity", Hidden).SetValue(assembly, new Activity());
                using var detach = bus.Subscribe<PartSocketDetachRequest>(e =>
                    typeof(Clean_AssemblySystem).GetMethod("OnDetachRequested", Hidden).Invoke(assembly, new object[] { e }));
                var domains = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem)
                    .GetField("_parts", Hidden).GetValue(assembly);
                var managerGO = new GameObject("Routing verification manager");
                SceneManager.MoveGameObjectToScene(managerGO, scene);
                var manager = managerGO.AddComponent<XRInteractionManager>();
                var plate = Create(plateConfig, "plate", scene, bus, registry, domains, manager);
                var detailConfig = configs.First(c => c.PartType == PartType.Motor && c.Prefab != null);
                var childConfig = configs.First(c => c.PartType == PartType.Propeller && c.Prefab != null);
                var detail = Create(detailConfig, "detail", scene, bus, registry, domains, manager);
                var child = Create(childConfig, "child", scene, bus, registry, domains, manager);
                var loose = Create(detailConfig, "loose", scene, bus, registry, domains, manager);
                Attach(detail, plate, domains);
                Attach(child, detail, domains);
                var ownPlate = plate.GrabInteractable.colliders.ToArray();
                var ownDetail = detail.GrabInteractable.colliders.ToArray();
                var ownChild = child.GrabInteractable.colliders.ToArray();
                Require(ownPlate.Length > 0 && ownDetail.Length > 0 && ownChild.Length > 0, "Missing test geometry.");
                assembly.RebuildDrones();
                Require(assembly.TryGetDroneForPart("child", out var drone) && drone.InstanceId == "plate", "Wrong root.");
                using (var mode = new AssemblyGrabModeService(bus, assembly, registry))
                {
                    mode.Initialize();
                    CheckOwner(manager, ownPlate, plate);
                    CheckOwner(manager, ownDetail, plate);
                    CheckOwner(manager, ownChild, plate);
                    CheckOwner(manager, loose.GrabInteractable.colliders, loose);
                    Require(!detail.Process((IXRHoverInteractor)null, null), "Installed detail hover not gated.");
                    var targets = new List<DronePartView>();
                    mode.GetHighlightTargets(plate, targets);
                    Require(targets.Count == 3, "Whole drone highlight membership incorrect.");
                    using (var highlight = new PartHighlightService(mode))
                    {
                        highlight.Initialize();
                        var left = new object();
                        var right = new object();
                        highlight.Enter(plate, left);
                        Require(IsHighlighted(plate) && IsHighlighted(detail) && IsHighlighted(child) && !IsHighlighted(loose), "Whole drone highlight failed.");
                        highlight.Enter(plate, right);
                        highlight.Exit(left);
                        Require(IsHighlighted(detail), "One hand cleared the other hand's hover.");
                        highlight.Exit(right);
                        Require(!IsHighlighted(plate) && !IsHighlighted(detail) && !IsHighlighted(child), "Highlight not cleared.");
                        bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = true });
                        CheckOwner(manager, ownPlate, plate);
                        CheckOwner(manager, ownDetail, detail);
                        CheckOwner(manager, ownChild, child);
                        highlight.Enter(detail, left);
                        Require(IsHighlighted(detail) && !IsHighlighted(plate) && !IsHighlighted(child), "Detail mode highlights whole drone.");
                        highlight.Exit(left);
                        Require(plate.GrabEnvelope == null || plate.GrabEnvelope.MainCollider.enabled, "Plate collider disabled in detail mode.");

                        // Mirror XRIT's selected state without running controller/physics updates.
                        typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(detail.GrabInteractable, true);
                        bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = false });
                        CheckOwner(manager, ownDetail, detail);
                        Require(detail.IsGrabbed, "Mode change canceled held detail.");
                        typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(detail.GrabInteractable, false);
                        bus.Publish(new PartTransformChangedEvent { instanceId = "detail" });
                        CheckOwner(manager, ownDetail, plate);

                        typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(plate.GrabInteractable, true);
                        bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = true });
                        CheckOwner(manager, ownDetail, plate);
                        Require(plate.IsGrabbed, "Mode change canceled held drone.");
                        typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(plate.GrabInteractable, false);
                        bus.Publish(new PartTransformChangedEvent { instanceId = "plate" });
                        CheckOwner(manager, ownDetail, detail);

                        // Detach one subtree, retaining its child relationship.
                        bus.Publish(new PartGrabStartedEvent { InstanceId = "detail" });
                        Require(domains["detail"].AttachedPartInstanceId == null && detail.transform.parent == null,
                            "Detail grab did not detach through the application handler.");
                        bus.Publish(new SetAssemblyDetailModeRequest { IsHeld = false });
                        CheckOwner(manager, ownPlate, plate);
                        CheckOwner(manager, ownDetail, detail);
                        CheckOwner(manager, ownChild, child);
                        Require(domains["child"].AttachedPartInstanceId == "detail", "Subtree lost child.");
                        mode.GetHighlightTargets(plate, targets);
                        Require(targets.Count == 1, "Detached detail still highlighted with drone.");
                        Attach(detail, plate, domains);
                        assembly.RebuildDrones();
                        CheckOwner(manager, ownDetail, plate);
                        CheckOwner(manager, ownChild, plate);
                    }
                }
                CheckOwner(manager, ownPlate, plate);
                CheckOwner(manager, ownDetail, detail);
                CheckOwner(manager, ownChild, child);
                Require(bus.GetSubscriberCount<SetAssemblyDetailModeRequest>() == 0, "Mode subscription leaked.");
                Require(logger.Errors == 0, "Event handler failed.");
                Debug.Log("Verified: " + plateConfig.PartId);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }

    private static DronePartView Create(PartConfig config, string id, Scene scene, IEventBus bus,
        PartViewRegistry registry, Dictionary<string, PartDomainState> domains, XRInteractionManager manager)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab, scene);
        typeof(XRGrabInteractable).GetMethod("Awake", Hidden).Invoke(go.GetComponent<XRGrabInteractable>(), null);
        var view = go.GetComponent<DronePartView>() ?? go.AddComponent<DronePartView>();
        typeof(DronePartView).GetMethod("Awake", Hidden).Invoke(view, null);
        view.Init(id, bus, config.HasConstantMaterial);
        registry.Register(id, go);
        domains.Add(id, new PartDomainState(id, config.PartId, config.PartType, new PartVisualProperties()));
        view.GrabInteractable.interactionManager = manager;
        typeof(XRBaseInteractable).GetMethod("OnEnable", Hidden).Invoke(view.GrabInteractable, null);
        return view;
    }

    private static void Attach(DronePartView child, DronePartView parent, Dictionary<string, PartDomainState> domains)
    {
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = new Vector3(0.5f, 0, 0);
        domains[child.InstanceId].AttachToPartSocket(parent.InstanceId, "verification");
    }

    private static bool IsHighlighted(DronePartView view) =>
        (bool)typeof(DronePartView).GetField("highlighted", Hidden).GetValue(view);

    private static void CheckOwner(XRInteractionManager manager, IEnumerable<Collider> colliders, DronePartView expected)
    {
        foreach (var collider in colliders)
            Require(manager.TryGetInteractableForCollider(collider, out var owner) &&
                ReferenceEquals(owner, expected.GrabInteractable), "Wrong collider owner: " + collider.name + " expected " + expected.InstanceId);
    }

    private static void Require(bool valid, string message) { if (!valid) throw new Exception(message); }
    private sealed class Activity : IUserActivityService
    {
        public float IdleTime => 0;
        public void NotifyActivity() { }
    }
    private sealed class Logger : IAppLogger
    {
        public int Errors;
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { Errors++; Debug.LogError(message); }
    }
}
#endif
