#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class RenderDistanceSettingsVerification
{
    private sealed class Logger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    [MenuItem("Tools/BAS VR/Verify Graphics Settings")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        VerifyMenus();
        var original = SceneManager.GetActiveScene();
        bool hadDistance = PlayerPrefs.HasKey(RenderDistancePolicy.PreferenceKey);
        float savedDistance = PlayerPrefs.GetFloat(RenderDistancePolicy.PreferenceKey);
        bool hadFog = PlayerPrefs.HasKey(RenderDistancePolicy.FogPreferenceKey);
        int savedFog = PlayerPrefs.GetInt(RenderDistancePolicy.FogPreferenceKey);
        var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scratch.name = "ConstructorScene";
        SceneManager.SetActiveScene(scratch);
        RenderTexture texture = null;
        try
        {
            PlayerPrefs.DeleteKey(RenderDistancePolicy.PreferenceKey);
            PlayerPrefs.DeleteKey(RenderDistancePolicy.FogPreferenceKey);
            var main = new GameObject("Test gameplay camera", typeof(Camera)).GetComponent<Camera>();
            var preview = new GameObject("Test UI preview", typeof(Camera)).GetComponent<Camera>();
            texture = new RenderTexture(16, 16, 0);
            preview.targetTexture = texture;
            preview.farClipPlane = .75f;
            using (var bus = new EventBus(new Logger(), new DiContainer()))
            {
                bus.Initialize();
                var container = new DiContainer();
                container.BindInstance<IEventBus>(bus);
                container.BindInterfacesTo<RenderDistanceSettings>().AsSingle().WithArguments(scratch);
                var settings = container.Resolve<IRenderDistanceSettings>();
                var service = (RenderDistanceSettings)settings;
                service.Initialize();
                try
                {
                    Check(main.farClipPlane == 120 && RenderSettings.fog, "Default distance/fog");
                    Check(Mathf.Approximately(RenderSettings.fogStartDistance, 40) && Mathf.Approximately(RenderSettings.fogEndDistance, 100), "Default fog range");
                    Check(preview.farClipPlane == .75f, "UI preview camera changed");
                    var drone = new GameObject("Test disabled drone camera", typeof(Camera)).GetComponent<Camera>();
                    drone.enabled = false;
                    settings.RegisterCamera(drone);
                    bus.Publish(new SetRenderDistanceRequest { Distance = 240 });
                    Check(main.farClipPlane == 240 && drone.farClipPlane == 240, "Immediate gameplay and disabled camera update");
                    Check(Mathf.Approximately(RenderSettings.fogStartDistance, 80) && Mathf.Approximately(RenderSettings.fogEndDistance, 200), "Proportional fog range");
                    bus.Publish(new SetFogEnabledRequest { Enabled = false });
                    Check(!RenderSettings.fog && main.farClipPlane == 240, "Fog off must preserve clipping");
                    bus.Publish(new SetRenderDistanceRequest { Distance = 3000 });
                    Check(settings.Distance == 300 && !RenderSettings.fog, "Upper limit and fog-off retention");
                    bus.Publish(new SetRenderDistanceRequest { Distance = -1 });
                    Check(settings.Distance == 60, "Lower limit");
                    bus.Publish(new SetRenderDistanceRequest { Distance = float.NaN });
                    Check(settings.Distance == 120, "Invalid input fallback");
                    bus.Publish(new SetRenderDistanceRequest { Distance = 180 });
                    var reloaded = new RenderDistanceSettings(bus, scratch);
                    Check(reloaded.Distance == 180 && !reloaded.FogEnabled, "Preferences reload");
                    reloaded.Dispose();
                    // Edit Mode has a different scene-activation event; invoke the runtime handler.
                    RenderSettings.fog = true;
                    RenderSettings.fogEndDistance = 999;
                    typeof(RenderDistanceSettings).GetMethod("OnActiveSceneChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(service, new object[] { original, scratch });
                    Check(!RenderSettings.fog && Mathf.Approximately(RenderSettings.fogEndDistance, 150), "Scene activation reapplies saved graphics settings");
                    bus.Publish(new SetFogEnabledRequest { Enabled = true });
                    Check(RenderSettings.fog, "Fog re-enable");
                    settings.UnregisterCamera(drone);
                }
                finally { service.Dispose(); }
                Check(bus.GetSubscriberCount<SetRenderDistanceRequest>() == 0 && bus.GetSubscriberCount<SetFogEnabledRequest>() == 0, "Subscription cleanup");
            }
        }
        finally
        {
            if (hadDistance) PlayerPrefs.SetFloat(RenderDistancePolicy.PreferenceKey, savedDistance);
            else PlayerPrefs.DeleteKey(RenderDistancePolicy.PreferenceKey);
            if (hadFog) PlayerPrefs.SetInt(RenderDistancePolicy.FogPreferenceKey, savedFog);
            else PlayerPrefs.DeleteKey(RenderDistancePolicy.FogPreferenceKey);
            PlayerPrefs.Save();
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scratch, true);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
        }
        Debug.Log("PASS: graphics menu references and XR canvases; DI; live clipping; proportional fog; fog toggle; disabled drone cameras; UI preview exclusion; limits; saved preferences; activation callback; disposal.");
    }

    private static void VerifyMenus()
    {
        foreach (var name in new[] { "BootMenu", "ConstructorScene", "FlightCoreScene" })
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/" + name + ".unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            try
            {
                var views = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RenderDistanceSettingsView>(true)).ToArray();
                Check(views.Length == 1, name + " must have exactly one graphics settings view");
                var so = new SerializedObject(views[0]);
                foreach (var field in new[] { "decreaseButton", "increaseButton", "resetButton", "distanceLabel", "fogToggle" })
                    Check(so.FindProperty(field).objectReferenceValue != null, name + ": missing " + field);
                var canvas = views[0].GetComponentInParent<Canvas>(true);
                Check(canvas != null && canvas.renderMode == RenderMode.WorldSpace, name + ": world-space canvas");
                Check(canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() != null, name + ": XR UI raycaster");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
