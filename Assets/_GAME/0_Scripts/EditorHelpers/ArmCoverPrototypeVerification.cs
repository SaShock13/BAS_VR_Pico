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

public static class ArmCoverPrototypeVerification
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/BAS VR/Verify R2 Arm Cover Prototype")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode verification only.");
        var scene = EditorSceneManager.NewPreviewScene();
        using var bus = new EventBus(new Logger(), new DiContainer());
        bus.Initialize();
        ArmCoverScaleService scales = null;
        try
        {
            var armConfig = AssetDatabase.LoadAssetAtPath<GameObject>(ArmCoverPrototypeSetup.ArmPath).GetComponent<PartViewConfigLink>().Config;
            var coverConfig = AssetDatabase.LoadAssetAtPath<PartConfig>(ArmCoverPrototypeSetup.ConfigPath);
            var registry = new PartViewRegistry(bus);
            var assembly = new Clean_AssemblySystem(bus, new Logger(), new PartConfigRepository(new[] { armConfig, coverConfig }),
                null, registry, new DiContainer(), null, null, null, null);
            typeof(Clean_AssemblySystem).GetField("_userActivity", Hidden).SetValue(assembly, new Activity());
            assembly.Initialize();
            var states = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts", Hidden).GetValue(assembly);
            var arm = Create(armConfig, "arm", scene, bus, assembly, registry, states);
            var cover = Create(coverConfig, "cover", scene, bus, assembly, registry, states);
            var socket = arm.GetSocketByType(PartType.ArmCover);
            Require(socket != null && socket.transform.localPosition == Vector3.zero && socket.transform.localRotation == Quaternion.identity,
                "Cover socket must use the arm pivot and axes.");
            bus.Publish(new PartSocketAttachRequest
            {
                PartInstanceId = "cover", AttachedPartId = "arm", AttachedSocketId = socket.SocketId, Timestamp = DateTime.UtcNow
            });
            Require(states["cover"].LifecycleState == PartLifecycleState.Installed && cover.transform.parent == socket.transform,
                "Cover did not attach through the ordinary assembly event.");
            Require(cover.transform.localPosition == Vector3.zero && cover.transform.localRotation == Quaternion.identity,
                "Cover did not snap to the arm pivot.");
            scales = new ArmCoverScaleService(assembly, registry, bus); scales.Initialize();
            int geometryChanges = 0, snapshots = 0;
            using var geometry = bus.Subscribe<AssemblyGeometryChangedEvent>(_ => geometryChanges++);
            using var snapshot = bus.Subscribe<AssemblyChangedEvent>(_ => snapshots++);
            void Scale(Vector3 value) => bus.Publish(new SetArmCoverScaleRequest { InstanceId = "cover", Scale = value, Timestamp = DateTime.UtcNow });
            Scale(new Vector3(1.2f, .8f, 1.5f));
            Require(cover.transform.localScale == new Vector3(1.2f, .8f, 1.5f) && cover.transform.localPosition == Vector3.zero,
                "Independent scale axes or anchored pivot failed.");
            bus.Publish(new SetArmCoverOffsetRequest { InstanceId = "cover", Offset = .017f, Timestamp = DateTime.UtcNow });
            Require(Mathf.Abs(cover.transform.localPosition.x - .017f) < .000001f && cover.transform.localPosition.y == 0 && cover.transform.localPosition.z == 0,
                "Along-arm adjustment changed another axis.");
            int beforeGeometry = geometryChanges, beforeSnapshots = snapshots;
            for (int i = 0; i < 20; i++)
            {
                scales.PreviewScale("cover", new Vector3(1.3f + i * .01f, .9f, 1.4f));
                scales.PreviewOffset("cover", .02f + i * .001f);
            }
            Require(geometryChanges == beforeGeometry && snapshots == beforeSnapshots, "Continuous drag published global events.");
            bus.Publish(new CommitArmCoverAdjustmentRequest { InstanceId = "cover", Timestamp = DateTime.UtcNow });
            Require(geometryChanges == beforeGeometry + 1 && snapshots == beforeSnapshots + 1, "Drag did not commit once.");
            var save = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(PartMapper.ToSaveData(states["cover"], cover.transform)));
            var savedPosition = cover.transform.localPosition; var savedScale = cover.transform.localScale;
            cover.transform.SetParent(null); cover.transform.localScale = Vector3.one; cover.transform.position = Vector3.one;
            PartMapper.ApplyToView(save, cover, registry);
            Require(cover.transform.parent == socket.transform && cover.transform.localPosition == savedPosition && cover.transform.localScale == savedScale,
                "Save/restore lost fitted size, longitudinal offset or parent socket.");
            var legacy = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(save));
            legacy.HasArmCoverOffset = false;
            Require(Mathf.Abs(PartMapper.ToDomain(legacy).ArmCoverOffset - savedPosition.x) < .000001f,
                "Legacy installed save did not recover its remembered offset.");
            bus.Publish(new PartSocketDetachRequest { PartInstanceId = "cover", Timestamp = DateTime.UtcNow });
            Require(!scales.CanMoveAlong("cover"), "Detached cover permits along-arm movement.");
            var freePosition = cover.transform.position;
            bus.Publish(new SetArmCoverOffsetRequest { InstanceId = "cover", Offset = -.08f, Timestamp = DateTime.UtcNow });
            Require(cover.transform.position == freePosition && states["cover"].ArmCoverOffset == savedPosition.x,
                "Detached offset request moved the cover or overwrote its fit.");
            var freeSave = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(PartMapper.ToSaveData(states["cover"], cover.transform)));
            states["cover"] = PartMapper.ToDomain(freeSave);
            cover.transform.position = new Vector3(3, 2, 1);
            bus.Publish(new PartSocketAttachRequest
            {
                PartInstanceId = "cover", AttachedPartId = "arm", AttachedSocketId = socket.SocketId, Timestamp = DateTime.UtcNow
            });
            Require(cover.transform.localPosition == savedPosition && scales.CanMoveAlong("cover"),
                "Reattachment after saving a detached cover lost its fitted offset.");
            Scale(new Vector3(float.NaN, 1, 1));
            Require(cover.transform.localScale == savedScale, "Non-finite scale was accepted.");
            Scale(new Vector3(-1, 100, 0));
            Require(cover.transform.localScale == new Vector3(.25f, 3, .25f), "Positive scale limits failed.");
            bus.Publish(new SetArmCoverScaleRequest { InstanceId = "arm", Scale = Vector3.one * 2, Timestamp = DateTime.UtcNow });
            Require(arm.transform.localScale == Vector3.one, "Cover tool changed a non-cover part.");
            typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(cover.GrabInteractable, true);
            Scale(Vector3.one);
            Require(cover.transform.localScale == new Vector3(.25f, 3, .25f), "Grabbed part was adjusted.");
            typeof(XRBaseInteractable).GetProperty("isSelected").SetValue(cover.GrabInteractable, false);
            VerifyHandles(scene, cover, scales, bus);
            scales.Dispose(); scales = null;
            Require(bus.GetSubscriberCount<SetArmCoverScaleRequest>() == 0 && bus.GetSubscriberCount<SetArmCoverOffsetRequest>() == 0 &&
                bus.GetSubscriberCount<CommitArmCoverAdjustmentRequest>() == 0, "Adjustment subscriptions leaked.");
        }
        finally { scales?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        Debug.Log("PASS: R2 cover attachment, independent scales, along-arm motion, drag preview/commit, undo snapshots, save restoration, finite/positive limits, type/held guards and subscription cleanup. No Play Mode entered.");
    }

    internal static void VerifyHandles(Scene scene, DronePartView cover, ArmCoverScaleService scales, IEventBus bus)
    {
        var root = new GameObject("CoverHandleVerification");
        SceneManager.MoveGameObjectToScene(root, scene);
        var owner = root.AddComponent<ArmCoverAdjustmentView>();
        void Set(string name, object value) => typeof(ArmCoverAdjustmentView).GetField(name, Hidden).SetValue(owner, value);
        var controls = new GameObject("Controls"); controls.transform.SetParent(root.transform, false);
        var text = new GameObject("Dimensions", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TMP_Text>();
        text.transform.SetParent(root.transform, false);
        var gizmo = new GameObject("Gizmo").transform; gizmo.SetParent(root.transform, false);
        var handles = new ArmCoverScaleHandle[5];
        for (int i = 0; i < handles.Length; i++)
        {
            var go = new GameObject("Handle" + i, typeof(BoxCollider)); go.transform.SetParent(gizmo, false);
            handles[i] = go.AddComponent<ArmCoverScaleHandle>();
            typeof(ArmCoverScaleHandle).GetField("axis", Hidden).SetValue(handles[i], i);
            handles[i].Configure(owner);
        }
        Set("controls", controls); Set("dimensions", text); Set("gizmoLabel", text); Set("gizmo", gizmo);
        Set("handles", handles); Set("_target", cover); Set("_geometry", cover.GetComponent<ArmCoverGeometry>());
        Set("_scales", scales); Set("_bus", bus); Set("_id", "cover");
        var buttons = new UnityEngine.UI.Button[3];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = new GameObject("AlongButton" + i, typeof(RectTransform), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
            buttons[i].transform.SetParent(root.transform, false);
        }
        Set("alongButtons", buttons);
        var heightButtons = new UnityEngine.UI.Button[2];
        for (int i = 0; i < heightButtons.Length; i++)
        {
            heightButtons[i] = new GameObject("HeightButton" + i, typeof(RectTransform), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
            heightButtons[i].transform.SetParent(root.transform, false);
        }
        Set("heightButtons", heightButtons);
        owner.ToggleGizmo();
        bool motorCover = scales.UsesUniformScaleOnly("cover");
        Require(heightButtons.All(b => b.interactable), "Installed cover height buttons disabled.");
        var hand = new GameObject("TestHand"); hand.transform.SetParent(root.transform, false);
        var interactor = hand.AddComponent<XRDirectInteractor>();
        for (int axis = 0; axis < 5; axis++)
        {
            if (motorCover && (axis == 0 || axis == 2))
            {
                Require(!owner.BeginDrag(handles[axis], axis, out _, out _, out _), "Motor cover exposes a per-axis handle.");
                continue;
            }
            cover.transform.localScale = axis == 4 && !motorCover ? new Vector3(1, 1.5f, .8f) : Vector3.one;
            cover.transform.localPosition = Vector3.zero;
            cover.transform.localRotation = Quaternion.Euler(0, 25, 0);
            hand.transform.position = Vector3.zero;
            var h = handles[axis];
            if (axis == 4 && !motorCover) owner.ToggleUniformGizmo();
            typeof(ArmCoverScaleHandle).GetMethod("OnSelectEntered", Hidden | BindingFlags.DeclaredOnly).Invoke(h,
                new object[] { new SelectEnterEventArgs { interactorObject = interactor, interactableObject = h } });
            var direction = axis == 4 ? cover.transform.TransformDirection(Vector3.one.normalized)
                : axis == 3 ? cover.transform.parent.TransformDirection(Vector3.right)
                : cover.transform.TransformDirection(axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward);
            hand.transform.position += direction * .002f;
            h.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
            if (axis == 3)
                Require(Mathf.Abs(cover.transform.localPosition.x - .002f) < .00001f && cover.transform.localScale == Vector3.one,
                    "XR movement handle did not follow the arm axis independently of cover rotation.");
            else if (axis == 4)
            {
                var result = cover.transform.localScale;
                Require(result.x > 1 && Mathf.Abs(result.y / result.x - (motorCover ? 1 : 1.5f)) < .00001f &&
                    Mathf.Abs(result.z / result.x - (motorCover ? 1 : .8f)) < .00001f, "Uniform XR handle lost axis proportions.");
                Require(cover.transform.localPosition == Vector3.zero, "Uniform XR handle moved the cover pivot.");
            }
            else
            {
                Require(cover.transform.localScale[axis] > 1 && cover.transform.localPosition == Vector3.zero,
                    "XR scale handle did not adjust the selected axis.");
                for (int other = 0; other < 3; other++) if (other != axis)
                    Require(cover.transform.localScale[other] == 1, "XR handle changed an unrelated scale axis.");
            }
            typeof(ArmCoverScaleHandle).GetMethod("OnSelectExited", Hidden | BindingFlags.DeclaredOnly).Invoke(h,
                new object[] { new SelectExitEventArgs { interactorObject = interactor, interactableObject = h } });
        }
        owner.ToggleUniformGizmo();
        Require(!gizmo.gameObject.activeSelf, "Gizmo did not switch off.");
        bus.Publish(new PartSocketDetachRequest { PartInstanceId = "cover", Timestamp = DateTime.UtcNow });
        typeof(ArmCoverAdjustmentView).GetMethod("Refresh", Hidden).Invoke(owner, null);
        Require(buttons.All(b => !b.interactable) && !handles[3].gameObject.activeSelf,
            "Detached cover did not disable along-arm buttons and hide its movement handle.");
        if (motorCover)
        {
            Require(heightButtons.All(b => !b.interactable) && !handles[1].gameObject.activeSelf,
                "Detached motor cover still exposes height controls.");
            var scale = cover.transform.localScale;
            scale.y += .1f;
            Require(!scales.PreviewScale("cover", scale), "Detached motor cover accepted a height change.");
        }
    }

    internal static DronePartView Create(PartConfig config, string id, Scene scene, IEventBus bus,
        Clean_AssemblySystem assembly, PartViewRegistry registry, Dictionary<string, PartDomainState> states)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(config.Prefab, scene);
        typeof(XRGrabInteractable).GetMethod("Awake", Hidden).Invoke(go.GetComponent<XRGrabInteractable>(), null);
        var view = go.AddComponent<DronePartView>();
        typeof(DronePartView).GetMethod("Awake", Hidden).Invoke(view, null);
        typeof(DronePartView).GetField("_assembly", Hidden).SetValue(view, assembly);
        foreach (var s in go.GetComponentsInChildren<SocketView>(true))
            typeof(SocketView).GetField("_previewSystem", Hidden).SetValue(s, new SocketPreviewSystem(null, null));
        // Keep material loading out of this Edit Mode geometry/integration test.
        view.Init(id, bus, true);
        typeof(DronePartView).GetMethod("OnEnable", Hidden).Invoke(view, null);
        registry.Register(id, go);
        states.Add(id, new PartDomainState(id, config.PartId, config.PartType, new PartVisualProperties()));
        return view;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Activity : IUserActivityService { public float IdleTime => 0; public void NotifyActivity() { } }
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
