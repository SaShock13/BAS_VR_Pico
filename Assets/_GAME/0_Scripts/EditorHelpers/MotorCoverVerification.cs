#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public static class MotorCoverVerification
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/BAS VR/Verify Motor Covers")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var covers = AssetDatabase.FindAssets("t:PartConfig", new[] { MotorCoverCatalogSetup.Configs })
            .Select(g => AssetDatabase.LoadAssetAtPath<PartConfig>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Require(covers.Length == 10, "Expected ten motor covers.");
        foreach (var cover in covers) VerifyCover(cover, cover == covers[0]);
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_GAME" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab.GetComponent<PartViewConfigLink>()?.Config?.PartType != PartType.Arm) continue;
            var sockets = prefab.GetComponentsInChildren<SocketView>(true);
            var motor = sockets.First(s => s.AllowedTypes.Contains(PartType.Motor));
            var cover = sockets.Single(s => s.AllowedTypes.Contains(PartType.MotorCover));
            Require(Vector3.Distance(motor.AttachPoint.position, cover.AttachPoint.position) < .00001f,
                "Motor cover socket not aligned to motor: " + prefab.name);
            var opening = cover.transform.TransformDirection(new Vector3(1, 0, -1).normalized);
            var inward = prefab.transform.position - cover.transform.position;
            inward.y = 0;
            Require(Vector3.Dot(opening.normalized, inward.normalized) > .999f,
                "Open motor-cover sector does not face inward along the arm: " + prefab.name);
            Require(sockets.Select(s => s.SocketId).Distinct().Count() == sockets.Length, "Duplicate socket ID.");
        }
        var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab");
        var bodywork = menu.GetComponentsInChildren<ObjectRowManager>(true).Single(r => r.name == "BodyworkParts");
        foreach (var config in covers)
            Require(bodywork.allObjects.Count(o => o.GetComponent<PartViewConfigLink>()?.Config == config) == 1,
                "Missing/duplicate motor cover in Bodywork menu.");
        VerifyPanelLayout();
        Debug.Log("PASS: ten motor covers attach through EventBus, scale/move/save/restore correctly; five XR handles including proportional XYZ; every arm has an aligned mount and every cover is in Bodywork.");
    }

    private static void VerifyPanelLayout()
    {
        var owner = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<ArmCoverAdjustmentView>(true)).FirstOrDefault();
        if (owner == null) return;
        var data = new SerializedObject(owner);
        var wrapper = (GameObject)data.FindProperty("controls").objectReferenceValue;
        var fitting = (GameObject)data.FindProperty("fittingContent").objectReferenceValue;
        HashSet<GameObject> Controls(string name)
        {
            var list = data.FindProperty(name);
            return Enumerable.Range(0, list.arraySize).Select(i => (GameObject)list.GetArrayElementAtIndex(i).objectReferenceValue).ToHashSet();
        }
        var axisControls = Controls("axisControls");
        var motorControls = Controls("motorScaleControls");
        Require(data.FindProperty("heightButtons").arraySize == 2, "Height buttons are not bound.");
        Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var points = corners.Select(p => wrapper.transform.InverseTransformPoint(p)).ToArray();
            return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
        }
        foreach (bool motor in new[] { false, true })
        {
            var rectangles = new List<(string name, Rect rect)>();
            foreach (var button in wrapper.GetComponentsInChildren<Button>(true))
            {
                if (motor ? axisControls.Contains(button.gameObject) : motorControls.Contains(button.gameObject)) continue;
                rectangles.Add((button.name, Bounds((RectTransform)button.transform)));
            }
            var hint = (TMPro.TMP_Text)data.FindProperty("fittingHint").objectReferenceValue;
            rectangles.Add(("Fitting hint", Bounds((RectTransform)hint.transform)));
            for (int i = 0; i < rectangles.Count; i++)
            for (int j = i + 1; j < rectangles.Count; j++)
                Require(!rectangles[i].rect.Overlaps(rectangles[j].rect),
                    "Cover panel overlap: " + rectangles[i].name + " / " + rectangles[j].name);
        }
    }

    private static void VerifyCover(PartConfig config, bool handles)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        using var bus = new EventBus(new Logger(), new DiContainer());
        bus.Initialize();
        ArmCoverScaleService scales = null;
        try
        {
            var armConfig = AssetDatabase.LoadAssetAtPath<GameObject>(ArmCoverPrototypeSetup.ArmPath).GetComponent<PartViewConfigLink>().Config;
            var registry = new PartViewRegistry(bus);
            var assembly = new Clean_AssemblySystem(bus, new Logger(),
                new PartConfigRepository(new[] { armConfig, config }), null, registry, new DiContainer(), null, null, null, null);
            typeof(Clean_AssemblySystem).GetField("_userActivity", Hidden).SetValue(assembly, new Activity());
            assembly.Initialize();
            var states = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts", Hidden).GetValue(assembly);
            var arm = ArmCoverPrototypeVerification.Create(armConfig, "arm", scene, bus, assembly, registry, states);
            var cover = ArmCoverPrototypeVerification.Create(config, "cover", scene, bus, assembly, registry, states);
            var socket = arm.GetSocketByType(PartType.MotorCover);
            scales = new ArmCoverScaleService(assembly, registry, bus);
            scales.Initialize();
            Require(scales.PreviewScale("cover", Vector3.one * 1.1f), "Free motor cover cannot be scaled.");
            Require(!scales.PreviewScale("cover", new Vector3(1.2f, 1.1f, 1.1f)), "Free motor cover accepted per-axis scaling.");
            cover.transform.localScale = new Vector3(1.2f, .8f, 1.4f); // A fitted cover from an older save.
            Require(scales.PreviewScale("cover", cover.transform.localScale * 1.1f), "Uniform adjustment of an older fitted cover failed.");
            bus.Publish(new SetArmCoverScaleRequest { InstanceId = "cover", ResetToDefault = true });
            Require(cover.transform.localScale == Vector3.one, "Motor cover reset failed before installation.");
            if (handles) VerifyFreePanel(scene, cover, scales, bus);
            Require(socket != null && assembly.CanAttach("cover", socket), "Motor cover mount rejected its cover.");
            Require(!assembly.IsCompatibleWithSocket("cover", arm.GetSocketByType(PartType.ArmCover)), "Motor cover accepted an arm-cover socket.");
            bus.Publish(new PartSocketAttachRequest
            {
                PartInstanceId = "cover", AttachedPartId = "arm", AttachedSocketId = socket.SocketId, Timestamp = DateTime.UtcNow
            });
            Require(states["cover"].LifecycleState == PartLifecycleState.Installed && cover.transform.parent == socket.transform,
                "Motor cover did not attach through application handler.");
            int snapshots = 0;
            using var changed = bus.Subscribe<AssemblyChangedEvent>(_ => snapshots++);
            var fit = Vector3.one * 1.2f;
            Require(!scales.PreviewScale("cover", new Vector3(1.2f, 1, 1)), "Installed motor cover accepted per-axis scaling.");
            Require(scales.CanScaleHeight("cover") && scales.PreviewScale("cover", new Vector3(1, 1.3f, 1)),
                "Installed motor cover rejected height scaling.");
            Require(cover.transform.localScale.x == 1 && cover.transform.localScale.z == 1, "Height changed the footprint.");
            bus.Publish(new SetArmCoverScaleRequest { InstanceId = "cover", ResetToDefault = true });
            snapshots = 0;
            Require(scales.PreviewScale("cover", fit) && scales.PreviewOffset("cover", .012f), "Motor cover fitting rejected.");
            Require(snapshots == 0, "Continuous fitting creates snapshots.");
            bus.Publish(new CommitArmCoverAdjustmentRequest { InstanceId = "cover", Timestamp = DateTime.UtcNow });
            Require(snapshots == 1, "Fitting did not commit exactly once.");
            var save = JsonUtility.FromJson<PartSaveData>(JsonUtility.ToJson(PartMapper.ToSaveData(states["cover"], cover.transform)));
            Require(save.Type == PartType.MotorCover && save.HasArmCoverOffset, "Motor cover persistence metadata missing.");
            cover.transform.SetParent(null);
            cover.transform.localScale = Vector3.one;
            PartMapper.ApplyToView(save, cover, registry);
            Require(cover.transform.parent == socket.transform && cover.transform.localScale == fit &&
                Mathf.Abs(cover.transform.localPosition.x - .012f) < .000001f, "Motor cover save/restore lost fit.");
            var uniform = ArmCoverScaleService.UniformScale(fit, 10);
            Require(uniform.x <= 3 && uniform.y <= 3 && uniform.z <= 3 &&
                Mathf.Abs(uniform.x / uniform.z - fit.x / fit.z) < .00001f, "Uniform limits changed proportions.");
            if (handles) ArmCoverPrototypeVerification.VerifyHandles(scene, cover, scales, bus);
        }
        finally { scales?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void VerifyFreePanel(Scene scene, DronePartView cover, ArmCoverScaleService scales, IEventBus bus)
    {
        var go = new GameObject("FreeMotorCoverPanelTest");
        SceneManager.MoveGameObjectToScene(go, scene);
        var panel = go.AddComponent<ArmCoverAdjustmentView>();
        void Set(string field, object value) => typeof(ArmCoverAdjustmentView).GetField(field, Hidden).SetValue(panel, value);
        Set("_target", cover); Set("_scales", scales); Set("_bus", bus); Set("_id", "cover");
        panel.UniformPlus();
        var enlarged = cover.transform.localScale;
        Require(enlarged.x > 1 && enlarged.x == enlarged.y && enlarged.x == enlarged.z,
            "Panel + button cannot uniformly scale a free motor cover.");
        panel.LengthPlus(); panel.HeightMinus(); panel.WidthPlus();
        Require(cover.transform.localScale == enlarged, "Hidden per-axis panel actions changed a motor cover.");
        panel.UniformMinus();
        var reduced = cover.transform.localScale;
        Require(reduced.x < enlarged.x && reduced.x == reduced.y && reduced.x == reduced.z,
            "Panel - button cannot uniformly scale a free motor cover.");
        panel.ResetScale();
        Require(cover.transform.localScale == Vector3.one, "Panel reset failed for a free motor cover.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
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
