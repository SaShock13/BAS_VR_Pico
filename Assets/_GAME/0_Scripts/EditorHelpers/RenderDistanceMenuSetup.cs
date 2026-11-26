#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RenderDistanceMenuSetup
{
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
        "Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");

    [MenuItem("Tools/BAS VR/Configure Graphics Settings Menus")]
    public static void ConfigureAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var original = SceneManager.GetActiveScene();
        try
        {
            foreach (var name in new[] { "BootMenu", "ConstructorScene", "FlightCoreScene" })
            {
                var path = "Assets/Scenes/" + name + ".unity";
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (name == "BootMenu") ConfigureBoot(Components<StartMenu>(scene).Single());
                    else if (name == "ConstructorScene") ConfigureAssembly(Components<LeftHandMenu>(scene).Single());
                    else ConfigureFlight(Components<FlightMenuView>(scene).Single());
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save " + path);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (original.isLoaded) SceneManager.SetActiveScene(original); }
        Debug.Log("Graphics settings configured in BootMenu, ConstructorScene and FlightCoreScene.");
    }

    public static void ConfigureBoot(StartMenu menu)
    {
        var page = (GameObject)new SerializedObject(menu).FindProperty("settingsWindow").objectReferenceValue;
        var info = page.transform.Find("Info");
        if (info != null) UnityEngine.Object.DestroyImmediate(info.gameObject);
        ConfigureControls(page.transform, new Vector2(0, 15), 700);
        var save = page.transform.Find("Save");
        var cancel = page.transform.Find("Cancel");
        save.GetComponentInChildren<TMP_Text>(true).text = "Готово";
        ((RectTransform)save).anchoredPosition = new Vector2(0, -260);
        ((RectTransform)save).sizeDelta = new Vector2(680, 64);
        cancel.gameObject.SetActive(false);
    }

    private static void ConfigureFlight(FlightMenuView menu)
    {
        var binding = new SerializedObject(menu);
        var page = (RectTransform)binding.FindProperty("_settingsPage").objectReferenceValue;
        page.sizeDelta = new Vector2(800, 820);
        Position((Component)binding.FindProperty("fpvToggle").objectReferenceValue, new Vector2(0, 315));
        Position((Component)binding.FindProperty("hudSettingsButton").objectReferenceValue, new Vector2(0, 225));
        Position((Component)binding.FindProperty("settingsBackButton").objectReferenceValue, new Vector2(0, -345));
        ConfigureControls(page, new Vector2(0, -30), 760);
        // The existing world-space Canvas and XR raycaster are reused.
    }

    private static void ConfigureAssembly(LeftHandMenu hand)
    {
        var binding = new SerializedObject(hand);
        var system = (GameObject)binding.FindProperty("systemMenu").objectReferenceValue;
        if (system == null) throw new InvalidOperationException("Assembly system menu is missing.");
        var canvas = system.transform.Find("Canvas");
        var settings = canvas.Find("GraphicsSettings");
        if (settings != null) UnityEngine.Object.DestroyImmediate(settings.gameObject);
        var previousButton = canvas.Find("OpenSettings");
        if (previousButton != null) UnityEngine.Object.DestroyImmediate(previousButton.gameObject);
        var page = Rect("GraphicsSettings", canvas, Vector2.zero, new Vector2(1060, 640));
        Label("Heading", page, "Настройки", new Vector2(0, 250), new Vector2(900, 65), 38);
        ConfigureControls(page, new Vector2(0, -10), 900);
        var back = Button("Back", page, "Назад", new Vector2(0, -275), new Vector2(650, 65));
        var open = Button("OpenSettings", canvas, "Настройки", new Vector2(0, -170), new Vector2(650, 65));
        var view = system.GetComponent<AssemblySettingsMenuView>();
        if (view == null) view = system.AddComponent<AssemblySettingsMenuView>();
        var so = new SerializedObject(view);
        so.FindProperty("settingsPage").objectReferenceValue = page.gameObject;
        var names = new[] { "Title", "ProjectSaveControls", "Close", "OpenGarage", "ClearAssembly", "OpenSettings" };
        var array = so.FindProperty("mainControls");
        array.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
        {
            var control = canvas.Find(names[i]);
            if (control == null) throw new InvalidOperationException("Missing assembly menu control: " + names[i]);
            array.GetArrayElementAtIndex(i).objectReferenceValue = control.gameObject;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(open.onClick, view.OpenSettings);
        UnityEventTools.AddPersistentListener(back.onClick, view.CloseSettings);
        page.gameObject.SetActive(false);
    }

    public static void ConfigureControls(Transform parent, Vector2 position, float width)
    {
        var existing = parent.Find("RenderDistanceControls");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var row = Rect("RenderDistanceControls", parent, position, new Vector2(width, 460));
        var value = Label("Distance", row, "Дальность прорисовки: 120 м", new Vector2(0, 140), new Vector2(width, 65), 32);
        var minus = Button("Decrease", row, "− 20 м", new Vector2(-width * .27f, 50), new Vector2(width * .42f, 75));
        var plus = Button("Increase", row, "+ 20 м", new Vector2(width * .27f, 50), new Vector2(width * .42f, 75));
        var reset = Button("Reset", row, "По умолчанию: 120 м", new Vector2(0, -40), new Vector2(width * .96f, 65));
        var toggleRect = Rect("Fog", row, new Vector2(0, -130), new Vector2(width * .96f, 70));
        var background = toggleRect.gameObject.AddComponent<Image>();
        background.color = new Color(.06f, .12f, .16f, 1);
        var toggle = toggleRect.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        var box = Rect("Checkbox", toggleRect, new Vector2(-width * .40f, 0), new Vector2(48, 48));
        box.gameObject.AddComponent<Image>().color = new Color(.18f, .30f, .35f, 1);
        var check = Rect("Checkmark", box, Vector2.zero, new Vector2(30, 30)).gameObject.AddComponent<Image>();
        check.color = new Color(.3f, .95f, .8f, 1);
        check.raycastTarget = false;
        toggle.graphic = check;
        toggle.isOn = true;
        Label("Label", toggleRect, "Туман", new Vector2(25, 0), new Vector2(width * .7f, 65), 32);
        Label("Hint", row, "Изменения применяются сразу и сохраняются", new Vector2(0, -200), new Vector2(width, 55), 23);
        var view = row.gameObject.AddComponent<RenderDistanceSettingsView>();
        var so = new SerializedObject(view);
        so.FindProperty("distanceLabel").objectReferenceValue = value;
        so.FindProperty("decreaseButton").objectReferenceValue = minus;
        so.FindProperty("increaseButton").objectReferenceValue = plus;
        so.FindProperty("resetButton").objectReferenceValue = reset;
        so.FindProperty("fogToggle").objectReferenceValue = toggle;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static void Position(Component component, Vector2 position) => ((RectTransform)component.transform).anchoredPosition = position;

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.08f, .16f, .20f, 1);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Label("Label", rect, text, Vector2.zero, size - new Vector2(12, 8), 30);
        return button;
    }
}
#endif
