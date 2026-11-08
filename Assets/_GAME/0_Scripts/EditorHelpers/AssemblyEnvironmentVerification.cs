#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public static class AssemblyEnvironmentVerification
{
    private sealed class Loader : ISceneLoader
    {
        public int Loads, Activations;
        public void Initialize() { }
        public void Dispose() { }
        public Task PreloadSceneAsync(string scene, Action callback, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Check(scene == MenuNavigationService.ConstructorSceneName && mode == LoadSceneMode.Single, "Wrong core load");
            Loads++;
            return Task.CompletedTask;
        }
        public Task ActivatePreloadedScene() { Activations++; return Task.CompletedTask; }
    }

    [MenuItem("Tools/BAS VR/Assembly/Verify Environments And Selection")]
    public static void Run()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode only");
        for (int i = 0; i < SceneManager.sceneCount; i++) Check(!SceneManager.GetSceneAt(i).isDirty, "Save open scenes first");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        string selected = AssemblyEnvironmentSession.EnvironmentId;
        var catalog = AssetDatabase.LoadAssetAtPath<AssemblyEnvironmentCatalog>("Assets/_GAME/Configs/AssemblyEnvironmentCatalog.asset");
        Check(catalog != null && catalog.Environments.Count >= 2, "Catalog missing");
        Check(catalog.Environments.All(e => e != null && !string.IsNullOrWhiteSpace(e.id)), "Empty ID");
        Check(catalog.Environments.Select(e => e.id).Distinct().Count() == catalog.Environments.Count, "Duplicate IDs");
        try
        {
            var core = EditorSceneManager.OpenScene("Assets/Scenes/ConstructorScene.unity", OpenSceneMode.Single);
            Check(Components<SceneContext>(core).Length == 1 && Components<Camera>(core).Length == 1
                && Components<EventSystem>(core).Length == 1, "Core XR/DI duplicated or missing");
            var bootstrap = Components<AssemblyEnvironmentBootstrap>(core).Single();
            Check(new SerializedObject(bootstrap).FindProperty("catalog").objectReferenceValue == catalog, "Bootstrap catalog missing");
            Check(core.GetRootGameObjects().Any(r => r.name == "Assembly Workspace" && r.transform.childCount == 6), "Workspace missing");
            foreach (var entry in catalog.Environments)
            {
                Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == entry.scenePath), "Scene not enabled in build: " + entry.scenePath);
                var env = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Additive);
                Check(Components<Camera>(env).Length == 0 && Components<SceneContext>(env).Length == 0
                    && Components<EventSystem>(env).Length == 0, "Environment contains core objects");
                foreach (var t in Components<Transform>(env))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing environment script");
                EditorSceneManager.CloseScene(env, true);
            }
            var boot = EditorSceneManager.OpenScene("Assets/Scenes/BootMenu.unity", OpenSceneMode.Single);
            var source = Components<AssemblyEnvironmentSelectionView>(boot).Single();
            var installer = Components<EventBusInstaller>(boot).Select(i => new SerializedObject(i))
                .Single(i => i.FindProperty("installMenuNavigation").boolValue);
            Check(installer.FindProperty("assemblyEnvironmentCatalog").objectReferenceValue == catalog, "Boot catalog missing");
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            var bus = new EventBus(new ConsoleLogger(), new DiContainer());
            bus.Initialize();
            try
            {
                var view = clone.GetComponent<AssemblyEnvironmentSelectionView>();
                var data = new SerializedObject(view);
                var panel = (GameObject)data.FindProperty("panel").objectReferenceValue;
                var group = (CanvasGroup)data.FindProperty("underlyingMenu").objectReferenceValue;
                Button Button(string name) => (Button)data.FindProperty(name).objectReferenceValue;
                view.Construct(bus, catalog);
                typeof(AssemblyEnvironmentSelectionView).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                string confirmed = null;
                int confirms = 0;
                using (var confirmation = bus.Subscribe<ConfirmAssemblyEnvironmentRequest>(e => { confirmed = e.EnvironmentId; confirms++; }))
                {
                    AssemblyEnvironmentSession.EnvironmentId = catalog.Environments[0].id;
                    bus.Publish(new OpenAssemblyEnvironmentSelection());
                    Check(panel.activeSelf && !group.interactable && confirms == 0, "Picker did not gate loading");
                    Button("next").onClick.Invoke();
                    Check(((TMP_Text)data.FindProperty("title").objectReferenceValue).text == catalog.Environments[1].title, "Next failed");
                    Button("cancel").onClick.Invoke();
                    Check(!panel.activeSelf && group.interactable && confirms == 0, "Cancel failed");
                    bus.Publish(new OpenAssemblyEnvironmentSelection());
                    Button("previous").onClick.Invoke();
                    Button("confirm").onClick.Invoke();
                    bus.Publish(new MenuNavigationStatusEvent { IsLoading = true });
                    Button("confirm").onClick.Invoke();
                    Check(confirms == 1, "Confirm/repeated-click guard failed");
                    Check(confirmed == catalog.Environments[catalog.Environments.Count - 1].id, "Wrong selected location");
                    Check(!Button("cancel").interactable && !Button("confirm").interactable, "Loading controls not locked");
                    bus.Publish(new MenuNavigationStatusEvent { Error = "Expected test error" });
                    Check(Button("confirm").interactable && ((TMP_Text)data.FindProperty("description").objectReferenceValue).text == "Expected test error", "Retry controls not restored");
                }
                var loader = new Loader();
                using (var navigation = new MenuNavigationService(bus, loader, new ConsoleLogger(), catalog))
                {
                    navigation.Initialize();
                    bus.Publish(new RequestStartConstructorEvent());
                    Check(panel.activeSelf && loader.Loads == 0, "Navigation bypassed picker");
                    bus.Publish(new ConfirmAssemblyEnvironmentRequest { EnvironmentId = "missing" });
                    Check(loader.Loads == 0, "Invalid location started load");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); bus.Dispose(); }
            MainMenuNavigationVerification.Run();
            Debug.Log("PASS: assembly scene separation, XR/DI counts, catalog/build scenes, picker, paging, cancel, confirm and repeated-click guard. No Play Mode or project save files changed.");
        }
        finally
        {
            AssemblyEnvironmentSession.EnvironmentId = selected;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
#endif
