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
        var setup = EditorSceneManager.GetSceneManagerSetup();
        string selectedId = FlightSession.EnvironmentId;
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
                EditorSceneManager.CloseScene(scene, true);
            }
            var constructor = EditorSceneManager.OpenScene("Assets/Scenes/ConstructorScene.unity", OpenSceneMode.Single);
            var source = Components<FlightEnvironmentSelectionView>(constructor).Single();
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            try
            {
                var view = clone.GetComponent<FlightEnvironmentSelectionView>();
                var so = new SerializedObject(view);
                var panel = (GameObject)so.FindProperty("panel").objectReferenceValue;
                var bus = new EventBus(new ConsoleLogger(), new DiContainer()); bus.Initialize();
                try
                {
                    FlightSession.EnvironmentId = catalog.Environments[0].id;
                    view.Construct(bus, catalog);
                    typeof(FlightEnvironmentSelectionView).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                    string confirmed = null;
                    using var subscription = bus.Subscribe<ConfirmFlightEnvironmentRequest>(e => confirmed = e.EnvironmentId);
                    bus.Publish(new OpenFlightEnvironmentSelection());
                    Check(panel.activeSelf, "Selection did not open.");
                    ((Button)so.FindProperty("next").objectReferenceValue).onClick.Invoke();
                    Check(((TMP_Text)so.FindProperty("title").objectReferenceValue).text.Contains(catalog.Environments[1].title), "Second location not rendered.");
                    ((Button)so.FindProperty("confirm").objectReferenceValue).onClick.Invoke();
                    Check(confirmed == catalog.Environments[1].id && !panel.activeSelf, "Wrong confirmed location.");
                    confirmed = null;
                    bus.Publish(new OpenFlightEnvironmentSelection());
                    ((Button)so.FindProperty("previous").objectReferenceValue).onClick.Invoke();
                    Check(((TMP_Text)so.FindProperty("title").objectReferenceValue).text.Contains(catalog.Environments[catalog.Environments.Count - 1].title), "Previous did not wrap.");
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
            FlightSession.EnvironmentId = selectedId;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
#endif
