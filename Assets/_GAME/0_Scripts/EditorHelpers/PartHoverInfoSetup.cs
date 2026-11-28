#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PartHoverInfoSetup
{
    public const string PrefabPath = "Assets/_GAME/Prefabs/UI/PartHoverInfoPanel.prefab";

    [MenuItem("Tools/BAS VR/Set Up Part Hover Info")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var prefab = AssetDatabase.LoadAssetAtPath<PartHoverInfoView>(PrefabPath);
        if (prefab == null) prefab = CreatePrefab();
        foreach (var path in new[] { "Assets/Scenes/ConstructorScene.unity", "Assets/Scenes/GarageScene.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var camera = objects.Select(x => x.GetComponent<Camera>()).First(x => x != null && x.CompareTag("MainCamera"));
            foreach (var installer in objects.Select(x => x.GetComponent<AssemblyInstaller>()).Where(x => x != null))
            {
                var binding = new SerializedObject(installer);
                binding.FindProperty("partHoverInfoPrefab").objectReferenceValue = prefab;
                binding.FindProperty("partInfoViewer").objectReferenceValue = camera.transform;
                binding.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        const string menuPath = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
        var menu = PrefabUtility.LoadPrefabContents(menuPath);
        try
        {
            foreach (var link in menu.GetComponentsInChildren<PartViewConfigLink>(true))
            {
                if (link.Config == null || link.GetComponent<Button>() == null) continue;
                var hover = link.GetComponent<PartInfoPreviewHover>() ?? link.gameObject.AddComponent<PartInfoPreviewHover>();
                var binding = new SerializedObject(hover);
                binding.FindProperty("config").objectReferenceValue = link.Config;
                binding.ApplyModifiedProperties();
                // The info card replaces the short tooltip on catalog cells.
                var tooltip = link.GetComponent<MenuTooltip>();
                if (tooltip != null) tooltip.enabled = false;
            }
            PrefabUtility.SaveAsPrefabAsset(menu, menuPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(menu); }
        SeedController("FlyghtController", "Полётный контроллер · 20×20 мм", "20×20");
        SeedController("FlyghtController_30_5", "Полётный контроллер · 30,5×30,5 мм", "30,5×30,5");
        AssetDatabase.SaveAssets();
    }

    private static void SeedController(string name, string title, string pitch)
    {
        var config = AssetDatabase.LoadAssetAtPath<PartConfig>("Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/Tech/" + name + "Config.asset");
        if (config == null) return;
        if (string.IsNullOrWhiteSpace(config.DisplayName)) config.DisplayName = title;
        if (string.IsNullOrWhiteSpace(config.Description)) config.Description = "Обрабатывает команды пилота и данные датчиков для управления полётом дрона.";
        if (config.InfoCharacteristics == null || config.InfoCharacteristics.Length == 0)
            config.InfoCharacteristics = new[] { new PartConfig.InfoCharacteristic { Label = "Крепление", Value = pitch, Unit = "мм" } };
        EditorUtility.SetDirty(config);
    }

    private static PartHoverInfoView CreatePrefab()
    {
        var root = new GameObject("PartHoverInfoPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image));
        try
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(420, 360); rect.pivot = new Vector2(.5f, 0);
            rect.localScale = Vector3.one * .00075f;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var image = root.GetComponent<Image>(); image.color = new Color(.145f, .098f, .047f, .96f); image.raycastTarget = false;
            image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_GAME/Prefabs/UI/PartHoverInfoDepth.mat");
            var view = root.AddComponent<PartHoverInfoView>();
            var binding = new SerializedObject(view);
            binding.FindProperty("title").objectReferenceValue = Label(root.transform, "Title", 25, new Color(1, .827f, .439f));
            binding.FindProperty("description").objectReferenceValue = Label(root.transform, "Description", 19, new Color(.90f, .94f, .96f));
            binding.FindProperty("characteristics").objectReferenceValue = Label(root.transform, "Characteristics", 18, new Color(.78f, .89f, .92f));
            binding.FindProperty("additionalInfo").objectReferenceValue = Label(root.transform, "Additional information", 18, new Color(.78f, .83f, .86f));
            binding.ApplyModifiedProperties();
            var rule = new GameObject("Amber top rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rule.transform.SetParent(root.transform, false);
            var line = (RectTransform)rule.transform; line.anchorMin = new Vector2(0, 1); line.anchorMax = Vector2.one;
            line.pivot = new Vector2(.5f, 1); line.sizeDelta = new Vector2(0, 3); line.anchoredPosition = Vector2.zero;
            line.anchoredPosition3D = Vector3.back;
            var accent = rule.GetComponent<Image>(); accent.color = new Color(1, .827f, .439f); accent.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        return AssetDatabase.LoadAssetAtPath<PartHoverInfoView>(PrefabPath);
    }

    private static TMP_Text Label(Transform parent, string name, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
        label.fontSize = size; label.color = color; label.raycastTarget = false; label.richText = false;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
        label.rectTransform.pivot = new Vector2(0, 1);
        label.rectTransform.anchoredPosition3D = Vector3.back;
        label.rectTransform.sizeDelta = new Vector2(380, 60);
        label.text = name;
        return label;
    }
}
#endif
