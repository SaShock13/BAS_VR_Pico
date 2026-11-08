#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class FlightEnvironmentSetup
{
    public const string CatalogPath = "Assets/_GAME/Configs/FlightEnvironmentCatalog.asset";
    public const string CorePath = "Assets/Scenes/FlightCoreScene.unity";
    public const string EnvironmentPath = "Assets/Scenes/FlightScene.unity";
    public const string SecondPath = "Assets/Scenes/FlightScene_Obstacles.unity";

    [MenuItem("Tools/BAS VR/Flight/Create Common Flight And Environments")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CorePath) != null)
            throw new InvalidOperationException("Flight scenes already split. Setup is a one-time migration.");
        var original = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var flight = SceneManager.GetSceneByPath(EnvironmentPath);
            if (flight.isLoaded) EditorSceneManager.CloseScene(flight, true);
            if (!AssetDatabase.CopyAsset(EnvironmentPath, CorePath)) throw new InvalidOperationException("Could not copy FlightScene.");
            if (!AssetDatabase.IsValidFolder("Assets/_GAME/Configs")) AssetDatabase.CreateFolder("Assets/_GAME", "Configs");
            var catalog = ScriptableObject.CreateInstance<FlightEnvironmentCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("environments"); entries.arraySize = 2;
            SetEntry(entries.GetArrayElementAtIndex(0), "training", "Учебная площадка", "Открытая площадка для свободного полёта.", EnvironmentPath);
            SetEntry(entries.GetArrayElementAtIndex(1), "obstacles", "Полоса препятствий", "Та же площадка с воротами и препятствиями для тренировки.", SecondPath);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            var core = EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Additive);
            var rig = core.GetRootGameObjects().Single(r => r.GetComponentsInChildren<Camera>(true).Length > 0);
            var playerPosition = rig.transform.position;
            var playerRotation = rig.transform.rotation;
            foreach (var root in core.GetRootGameObjects())
                if (IsEnvironment(root)) UnityEngine.Object.DestroyImmediate(root);
            var bootstrap = Components<FlightSceneBootsttrap>(core).Single();
            var binding = new SerializedObject(bootstrap);
            binding.FindProperty("environmentCatalog").objectReferenceValue = catalog;
            binding.FindProperty("playerRoot").objectReferenceValue = rig.transform;
            binding.FindProperty("spawnPointTransform").objectReferenceValue = null;
            binding.ApplyModifiedPropertiesWithoutUndo();
            AssignCatalog(core, catalog);
            foreach (var tester in Components<PhysicsBuilderTester>(core)) tester.enabled = false;
            EditorSceneManager.SaveScene(core);
            EditorSceneManager.CloseScene(core, true);

            var env = EditorSceneManager.OpenScene(EnvironmentPath, OpenSceneMode.Additive);
            foreach (var root in env.GetRootGameObjects())
                if (!IsEnvironment(root)) UnityEngine.Object.DestroyImmediate(root);
            foreach (var grab in Components<XRGrabInteractable>(env)) UnityEngine.Object.DestroyImmediate(grab);
            foreach (var body in Components<Rigidbody>(env)) UnityEngine.Object.DestroyImmediate(body);
            var contextObject = new GameObject("FlightEnvironment");
            SceneManager.MoveGameObjectToScene(contextObject, env);
            var context = contextObject.AddComponent<FlightEnvironmentContext>();
            var player = new GameObject("PlayerSpawn").transform;
            player.SetParent(context.transform); player.SetPositionAndRotation(playerPosition, playerRotation);
            context.playerSpawn = player;
            context.droneSpawn = env.GetRootGameObjects().Single(r => r.name == "SpawnPoint").transform;
            EditorSceneManager.SaveScene(env);
            EditorSceneManager.CloseScene(env, true);
            if (!AssetDatabase.CopyAsset(EnvironmentPath, SecondPath)) throw new InvalidOperationException("Could not copy environment.");
            var second = EditorSceneManager.OpenScene(SecondPath, OpenSceneMode.Additive);
            var obstacleRoot = new GameObject("Training Obstacles"); SceneManager.MoveGameObjectToScene(obstacleRoot, second);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(.12f, .5f, .65f);
            AssetDatabase.CreateAsset(material, "Assets/_GAME/Configs/FlightObstacle.mat");
            for (int i = 0; i < 3; i++)
            {
                float z = 7 + i * 6;
                Obstacle(obstacleRoot.transform, material, "Gate Left " + i, new Vector3(-2, 1.5f, z), new Vector3(.4f, 3, .4f));
                Obstacle(obstacleRoot.transform, material, "Gate Right " + i, new Vector3(2, 1.5f, z), new Vector3(.4f, 3, .4f));
                Obstacle(obstacleRoot.transform, material, "Gate Top " + i, new Vector3(0, 3, z), new Vector3(4.4f, .4f, .4f));
            }
            EditorSceneManager.SaveScene(second); EditorSceneManager.CloseScene(second, true);

            var constructor = SceneManager.GetSceneByPath("Assets/Scenes/ConstructorScene.unity");
            bool closeConstructor = !constructor.isLoaded;
            if (closeConstructor) constructor = EditorSceneManager.OpenScene("Assets/Scenes/ConstructorScene.unity", OpenSceneMode.Additive);
            AssignCatalog(constructor, catalog);
            CreateSelection(Components<FlightModeView>(constructor).Single());
            EditorSceneManager.SaveScene(constructor);
            if (closeConstructor) EditorSceneManager.CloseScene(constructor, true);
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var path in new[] { CorePath, EnvironmentPath, SecondPath })
            {
                var existing = scenes.Find(s => s.path == path);
                if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true)); else existing.enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
        Debug.Log("Common flight scene, two environments and selection menu created.");
    }

    private static bool IsEnvironment(GameObject root) => root.name == "Environment" || root.name == "Directional Light" || root.name == "Global Volume" || root.name == "SpawnPoint";
    private static IEnumerable<T> Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    private static void SetEntry(SerializedProperty p, string id, string title, string description, string path)
    {
        p.FindPropertyRelative("id").stringValue = id;
        p.FindPropertyRelative("title").stringValue = title;
        p.FindPropertyRelative("description").stringValue = description;
        p.FindPropertyRelative("scenePath").stringValue = path;
    }
    private static void AssignCatalog(Scene scene, FlightEnvironmentCatalog catalog)
    {
        foreach (var installer in Components<AssemblyInstaller>(scene))
        {
            var so = new SerializedObject(installer);
            so.FindProperty("flightEnvironmentCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static void Obstacle(Transform parent, Material material, string name, Vector3 position, Vector3 scale)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name; cube.transform.SetParent(parent, false);
        cube.transform.position = position; cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.isStatic = true;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = 5; rect.SetParent(parent, false);
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static TMP_Text Text(Transform parent, string name, string value, Vector2 size, Vector2 position, float fontSize)
    {
        var text = Rect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
        text.text = value; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, string value, Vector2 position, float width)
    {
        var rect = Rect(parent, name, new Vector2(width, 82), position);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .2f, .26f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text(rect, "Label", value, new Vector2(width - 12, 78), Vector2.zero, 30);
        return button;
    }
    private static void CreateSelection(FlightModeView flight)
    {
        var view = flight.gameObject.AddComponent<FlightEnvironmentSelectionView>();
        var panel = Rect(flight.transform, "EnvironmentSelection", new Vector2(1100, 510), new Vector2(0, 180));
        panel.anchoredPosition3D = new Vector3(0, 180, -5);
        panel.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .06f, 1);
        Text(panel, "Header", "ВЫБОР ЛОКАЦИИ", new Vector2(1020, 70), new Vector2(0, 205), 38);
        var title = Text(panel, "Location", "", new Vector2(840, 90), new Vector2(0, 95), 40);
        var description = Text(panel, "Description", "", new Vector2(940, 120), new Vector2(0, -20), 30);
        var previous = Button(panel, "Previous", "<", new Vector2(-480, 95), 90);
        var next = Button(panel, "Next", ">", new Vector2(480, 95), 90);
        var confirm = Button(panel, "Confirm", "Начать полёт", new Vector2(245, -175), 450);
        var cancel = Button(panel, "Cancel", "Отмена", new Vector2(-245, -175), 450);
        var so = new SerializedObject(view);
        foreach (var pair in new[] { ("panel", (UnityEngine.Object)panel.gameObject), ("title", title), ("description", description), ("previous", previous), ("next", next), ("confirm", confirm), ("cancel", cancel) })
            so.FindProperty(pair.Item1).objectReferenceValue = pair.Item2;
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
    }
}
#endif
