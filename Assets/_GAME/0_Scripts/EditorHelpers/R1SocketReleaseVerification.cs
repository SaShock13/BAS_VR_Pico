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
using Zenject;

public static class R1SocketReleaseVerification
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Root = "Assets/_GAME/Prefabs/NewParts/";

    [MenuItem("Tools/BAS VR/Verify R1 Socket Release")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode verification only.");
        VerifyRelease(false, false, false);
        VerifyRelease(true, false, false);
        VerifyRelease(false, true, false);
        VerifyRelease(false, false, true);
        Debug.Log("PASS: R1 release reaches Installed through EventBus; hover-exit ordering, canceled release, second-hand selection and socket-ID lookup verified.");
    }

    private static void VerifyRelease(bool hoverExitedFirst, bool canceled, bool stillHeld)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        using var bus = new EventBus(new Logger(), new DiContainer());
        bus.Initialize();
        try
        {
            var plateConfig = Load("BottomPlate/R1_FrameBottomPlate.prefab");
            var armConfig = Load("Arm/R1_Frame_Arm5.prefab");
            var registry = new PartViewRegistry(bus);
            var assembly = new Clean_AssemblySystem(bus, new Logger(),
                new PartConfigRepository(new[] { plateConfig, armConfig }), null,
                registry, new DiContainer(), null, null, null, null);
            typeof(Clean_AssemblySystem).GetField("_userActivity", Hidden).SetValue(assembly, new Activity());
            assembly.Initialize();
            var states = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts", Hidden).GetValue(assembly);
            var plate = Create(plateConfig, "plate", scene, bus, assembly, registry, states);
            var arm = Create(armConfig, "arm", scene, bus, assembly, registry, states);
            var socket = plate.GetComponentsInChildren<SocketView>(true).First(s => s.AllowedTypes.Contains(PartType.Arm));
            Require(plate.GetSocket(socket.SocketId) == socket, "Socket registration failed.");
            arm.transform.position = socket.AttachPoint.position + Vector3.up * 0.012f;
            SetSelected(arm, true);
            var mount = socket.GetComponent<HoverOnlySocketInteractor>();
            Require(mount.CanHover((IXRHoverInteractable)arm.GrabInteractable), "12 mm hand tolerance rejected.");
            int candidates = plate.GetComponentsInChildren<SocketView>(true)
                .Count(s => s.AllowedTypes.Contains(PartType.Arm) &&
                    s.GetComponent<HoverOnlySocketInteractor>().CanHover((IXRHoverInteractable)arm.GrabInteractable));
            Require(candidates == 1, "Expected one nearest arm socket.");
            // The candidate is the result of the hover check above. No ghost meshes are created
            // in Edit Mode; the production release event and application commit run unchanged.
            arm.AddHoveredSocket(socket);
            typeof(SocketView).GetField("_hoveredPart", Hidden).SetValue(socket, arm);
            SetSelected(arm, stillHeld);
            if (hoverExitedFirst)
                typeof(SocketView).GetMethod("OnHoverExited", Hidden).Invoke(socket,
                    new object[] { new HoverExitEventArgs { interactableObject = arm.GrabInteractable } });
            int requests = 0;
            using var count = bus.Subscribe<PartSocketAttachRequest>(_ => requests++);
            arm.GrabInteractable.selectExited.Invoke(new SelectExitEventArgs
            {
                interactableObject = arm.GrabInteractable,
                isCanceled = canceled
            });
            bool expected = !canceled && !stillHeld;
            Require((states["arm"].LifecycleState == PartLifecycleState.Installed) == expected,
                "Release did not produce the expected domain state.");
            Require(requests == (expected ? 1 : 0), "Wrong attachment request count.");
            if (expected)
            {
                Require(states["arm"].AttachedPartInstanceId == "plate" &&
                    states["arm"].AttachedSocketId == socket.SocketId, "Wrong installed mount.");
                Require(arm.transform.parent == socket.transform && arm.transform.localPosition == Vector3.zero,
                    "View did not snap to the selected socket.");
            }
            SetSelected(arm, false);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static PartConfig Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + path)
        .GetComponent<PartViewConfigLink>().Config;

    private static DronePartView Create(PartConfig config, string id, Scene scene, IEventBus bus,
        Clean_AssemblySystem assembly, PartViewRegistry registry, Dictionary<string, PartDomainState> states)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab, scene);
        typeof(XRGrabInteractable).GetMethod("Awake", Hidden).Invoke(go.GetComponent<XRGrabInteractable>(), null);
        var view = go.AddComponent<DronePartView>();
        typeof(DronePartView).GetMethod("Awake", Hidden).Invoke(view, null);
        typeof(DronePartView).GetField("_assembly", Hidden).SetValue(view, assembly);
        foreach (var socket in go.GetComponentsInChildren<SocketView>(true))
            typeof(SocketView).GetField("_previewSystem", Hidden).SetValue(socket, new SocketPreviewSystem(null, null));
        view.Init(id, bus, config.HasConstantMaterial);
        typeof(DronePartView).GetMethod("OnEnable", Hidden).Invoke(view, null);
        registry.Register(id, go);
        states.Add(id, new PartDomainState(id, config.PartId, config.PartType, new PartVisualProperties()));
        return view;
    }

    private static void SetSelected(DronePartView view, bool selected) =>
        typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(view.GrabInteractable, selected);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Activity : IUserActivityService
    {
        public float IdleTime => 0;
        public void NotifyActivity() { }
    }

    private sealed class Logger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) => throw new InvalidOperationException(message);
    }
}
#endif
