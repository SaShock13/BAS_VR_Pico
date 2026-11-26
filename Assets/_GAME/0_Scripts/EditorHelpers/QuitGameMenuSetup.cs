#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class QuitGameMenuSetup
{
    public static void Verify()
    {
        using (var bus = new EventBus(new VerificationLogger(), new Zenject.DiContainer()))
        {
            bus.Initialize();
            int published = 0;
            using (bus.Subscribe<QuitGameRequest>(_ => published++))
            foreach (var view in UnityEngine.Object.FindObjectsByType<QuitGameView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var inactive = new System.Collections.Generic.List<GameObject>();
                try
                {
                    for (var t = view.transform; t != null; t = t.parent)
                        if (!t.gameObject.activeSelf) { inactive.Add(t.gameObject); t.gameObject.SetActive(true); }
                    view.Construct(bus);
                    var confirmation = view.GetComponent<ConfirmationView>();
                    var backdrop = view.transform.Find("Backdrop").gameObject;
                    int before = published;
                    view.Request();
                    if (!backdrop.activeSelf || published != before) throw new Exception("Request must only open dialog");
                    confirmation.Cancel();
                    confirmation.Confirm();
                    if (backdrop.activeSelf || published != before) throw new Exception("Cancel published quit");
                    view.Request();
                    confirmation.Confirm();
                    confirmation.Confirm();
                    if (published != before + 1 || backdrop.activeSelf) throw new Exception("Confirm must publish exactly once");
                    Debug.Log("Quit confirmation verified: " + view.gameObject.scene.name);
                }
                finally
                {
                    view.GetComponent<ConfirmationView>().Cancel();
                    foreach (var go in inactive) go.SetActive(false);
                }
            }
        }
    }

    private sealed class VerificationLogger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    [MenuItem("Tools/BAS VR/Set Up Quit Confirmation")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        foreach (var path in new[] { "Assets/Scenes/BootMenu.unity", "Assets/Scenes/ConstructorScene.unity", "Assets/Scenes/GarageScene.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            if (path.Contains("BootMenu"))
            {
                var menu = objects.Select(x => x.GetComponent<StartMenu>()).First(x => x != null);
                var view = CreateConfirmation((RectTransform)menu.transform.Find("AssemblyProjects"), false);
                var binding = new SerializedObject(menu);
                binding.FindProperty("quitGame").objectReferenceValue = view;
                binding.ApplyModifiedProperties();
            }
            else
            {
                bool assembly = path.Contains("ConstructorScene");
                var canvas = objects.First(x => assembly ? x.name == "Canvas" && x.parent != null && x.parent.name == "AssemblySystemMenu" : x.name == "GarageHandCanvas");
                var rect = (RectTransform)canvas;
                var view = CreateConfirmation(rect, assembly);
                var old = canvas.Find("QuitGame");
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                if (assembly)
                {
                    foreach (var name in new[] { "Close", "OpenGarage", "ClearAssembly" })
                    {
                        var row = (RectTransform)canvas.Find(name);
                        Undo.RecordObject(row, "Make room for quit action");
                        row.anchoredPosition = new Vector2(row.anchoredPosition.x, -205);
                    }
                    var settings = (RectTransform)canvas.Find("OpenSettings");
                    Undo.RecordObject(settings, "Move settings row");
                    settings.anchoredPosition = new Vector2(0, -130);
                }
                Button(rect, "QuitGame", "ВЫХОД ИЗ ИГРЫ", assembly ? new Vector2(0, -280) : new Vector2(0, -3),
                    assembly ? new Vector2(650, 56) : new Vector2(16, 1.4f), assembly ? 25 : .6f, view.Request);
                if (assembly)
                {
                    var settingsView = objects.Select(x => x.GetComponent<AssemblySettingsMenuView>()).First(x => x != null);
                    var settingsBinding = new SerializedObject(settingsView);
                    var controls = settingsBinding.FindProperty("mainControls");
                    var quit = canvas.Find("QuitGame").gameObject;
                    // Rebuilds replace the old quit button, leaving a null entry.
                    int slot = -1;
                    for (int i = 0; i < controls.arraySize; i++)
                        if (controls.GetArrayElementAtIndex(i).objectReferenceValue == null) { slot = i; break; }
                    if (slot < 0) { slot = controls.arraySize; controls.arraySize++; }
                    controls.GetArrayElementAtIndex(slot).objectReferenceValue = quit;
                    settingsBinding.ApplyModifiedProperties();
                }
                view.transform.SetAsLastSibling();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static QuitGameView CreateConfirmation(RectTransform parent, bool warnUnsaved)
    {
        var previous = parent.Find("QuitConfirmation");
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        var host = Rect("QuitConfirmation", parent, Vector2.zero, parent.rect.size);
        Stretch(host);
        var view = Undo.AddComponent<QuitGameView>(host.gameObject);
        var confirmation = Undo.AddComponent<ConfirmationView>(host.gameObject);
        var backdrop = Rect("Backdrop", host, Vector2.zero, parent.rect.size);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Image>().color = new Color(.015f, .025f, .04f, .98f);
        float unit = Mathf.Min(parent.rect.width / 1100f, parent.rect.height / 650f);
        var dialog = Rect("Dialog", backdrop, Vector2.zero, new Vector2(950, 300) * unit);
        var text = Label("Message", dialog, "Выйти из игры?", new Vector2(0, 65) * unit, new Vector2(870, 125) * unit, 32 * unit);
        Button(dialog, "Confirm", "ВЫЙТИ", new Vector2(-220, -75) * unit, new Vector2(390, 78) * unit, 28 * unit, confirmation.Confirm);
        Button(dialog, "Cancel", "ОТМЕНА", new Vector2(220, -75) * unit, new Vector2(390, 78) * unit, 28 * unit, confirmation.Cancel);
        var binding = new SerializedObject(confirmation);
        binding.FindProperty("panel").objectReferenceValue = backdrop.gameObject;
        binding.FindProperty("message").objectReferenceValue = text;
        binding.ApplyModifiedProperties();
        binding = new SerializedObject(view);
        binding.FindProperty("confirmation").objectReferenceValue = confirmation;
        binding.FindProperty("message").stringValue = warnUnsaved ? "Выйти из игры?\nНесохранённые изменения могут быть потеряны." : "Выйти из игры?";
        binding.ApplyModifiedProperties();
        backdrop.gameObject.SetActive(false);
        return view;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add quit confirmation UI");
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
        label.text = value; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void Button(RectTransform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, UnityAction action)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .16f, .2f, 1);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        Label("Label", rect, value, Vector2.zero, size * .94f, fontSize);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
#endif
