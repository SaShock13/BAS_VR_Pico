#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public static class FlightEnvironmentVerification
{
    [MenuItem("Tools/BAS VR/Flight/Verify Environments And Selection")]
    public static void Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FlightEnvironmentCatalog>(FlightEnvironmentSetup.CatalogPath);
        FlightEnvironmentBuildScenes.Validate(catalog);
        Check(catalog.Environments.Count >= 2, "Expected two environments.");
        Check(catalog.Missions.Count >= 2, "Expected two missions.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var selectedMission = FlightSession.Mission;
        try
        {
            var core = EditorSceneManager.OpenScene(catalog.coreScenePath, OpenSceneMode.Single);
            Check(Components<SceneContext>(core).Length == 1, "Core needs one SceneContext.");
            Check(Components<Camera>(core).Length == 1, "Core needs one XR camera.");
            Check(Components<FlightSceneBootsttrap>(core).Length == 1, "Core bootstrap missing.");
            var bootstrap = new SerializedObject(Components<FlightSceneBootsttrap>(core).Single());
            Check(bootstrap.FindProperty("playerRoot").objectReferenceValue != null, "Player root reference missing.");
            Check(bootstrap.FindProperty("environmentCatalog").objectReferenceValue == catalog, "Bootstrap catalog missing.");
            foreach (var entry in catalog.Environments)
            {
                Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == entry.scenePath), "Environment not enabled for build.");
                var scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Additive);
                Check(Components<SceneContext>(scene).Length == 0 && Components<Camera>(scene).Length == 0, "Environment contains core objects.");
                var context = Components<FlightEnvironmentContext>(scene).Single();
                Check(context.playerSpawn != null && context.droneSpawn != null, "Spawn points missing.");
                Check(context.playerSpawn.gameObject.scene == scene && context.droneSpawn.gameObject.scene == scene, "Cross-scene spawn reference.");
                Check(Components<Transform>(scene).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Missing environment script.");
                foreach (var mission in catalog.Missions.Where(m => m.environmentId == entry.id))
                    VerifyMission(mission.mission, context);
                EditorSceneManager.CloseScene(scene, true);
            }
            var constructor = EditorSceneManager.OpenScene("Assets/Scenes/ConstructorScene.unity", OpenSceneMode.Single);
            var source = Components<FlightEnvironmentSelectionView>(constructor).Single(view =>
                string.IsNullOrEmpty(new SerializedObject(view).FindProperty("panelId").stringValue));
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            try
            {
                var view = clone.GetComponent<FlightEnvironmentSelectionView>();
                var so = new SerializedObject(view);
                var panel = (GameObject)so.FindProperty("panel").objectReferenceValue;
                var bus = new EventBus(new ConsoleLogger(), new DiContainer()); bus.Initialize();
                try
                {
                    FlightSession.Mission = catalog.Missions[0].mission;
                    view.Construct(bus, catalog);
                    typeof(FlightEnvironmentSelectionView).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                    string confirmed = null;
                    using var subscription = bus.Subscribe<ConfirmFlightMissionRequest>(e => confirmed = e.MissionId);
                    bus.Publish(new OpenFlightMissionSelection());
                    Check(panel.activeSelf, "Selection did not open.");
                    ((Button)so.FindProperty("next").objectReferenceValue).onClick.Invoke();
                    Check(((TMP_Text)so.FindProperty("title").objectReferenceValue).text.Contains(catalog.Missions[1].mission.MissionName), "Second mission not rendered.");
                    Check(((TMP_Text)so.FindProperty("description").objectReferenceValue).text.Contains(
                        catalog.Find(catalog.Missions[1].environmentId).title), "Mission location not rendered.");
                    ((Button)so.FindProperty("confirm").objectReferenceValue).onClick.Invoke();
                    Check(confirmed == catalog.Missions[1].mission.MissionId && !panel.activeSelf, "Wrong confirmed mission.");
                    confirmed = null;
                    bus.Publish(new OpenFlightMissionSelection());
                    ((Button)so.FindProperty("previous").objectReferenceValue).onClick.Invoke();
                    Check(((TMP_Text)so.FindProperty("title").objectReferenceValue).text.Contains(catalog.Missions[catalog.Missions.Count - 1].mission.MissionName), "Previous did not wrap.");
                    ((Button)so.FindProperty("cancel").objectReferenceValue).onClick.Invoke();
                    Check(confirmed == null && !panel.activeSelf, "Cancel launched flight.");
                }
                finally { UnityEngine.Object.DestroyImmediate(clone); bus.Dispose(); }
            }
            finally { if (clone != null) UnityEngine.Object.DestroyImmediate(clone); }
            Debug.Log("PASS: flight scene separation, spawn references, catalog/build scenes, selection, confirm, cancel and paging.");
        }
        finally
        {
            FlightSession.Mission = selectedMission;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static void VerifyMission(MissionDefinition mission, FlightEnvironmentContext environment)
    {
        Check(environment.missionStart != null && environment.missionPoints != null,
            "Missing mission points or start zone.");
        var drone = new GameObject("Mission verification drone");
        try
        {
            var binder = drone.AddComponent<SceneMissionBinder>();
            typeof(SceneMissionBinder).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(binder, null);
            binder.BindFlightEnvironment(environment);
            var controller = drone.AddComponent<MissionController>();
            var runtime = MissionFactory.CreateRuntime(mission, binder);
            var tick = typeof(MissionController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            drone.transform.position = environment.droneSpawn.position;
            controller.ArmFlightMission(runtime, drone.transform, environment.missionStart, environment.missionStartRadius);
            tick.Invoke(controller, null);
            Check(controller.State == MissionState.Inactive, "Mission started at spawn.");
            drone.transform.position = environment.missionStart.position;
            tick.Invoke(controller, null);
            Check(controller.State == MissionState.Running, "Mission did not start at its start zone.");
            while (runtime.CurrentStep is MissionStep step)
            {
                if (step is WaitStep)
                    typeof(WaitStep).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(step, float.MaxValue);
                else
                {
                    Check(step.Target != null, "Missing mission step target.");
                    drone.transform.position = step.Target.position;
                }
                tick.Invoke(controller, null);
                Check(runtime.CurrentStep != step, "Mission step did not advance.");
            }
            Check(controller.State == MissionState.Completed, "Mission did not complete.");
            controller.StopMission();
            Debug.Log("PASS: start, steps and completion for " + mission.MissionName);
        }
        finally { UnityEngine.Object.DestroyImmediate(drone); }
    }

    private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
#endif
