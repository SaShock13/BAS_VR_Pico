#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class AssemblyMenuBuilder
{
    private const string PrefabPath = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
    // UI authoring units. HandMenuTransform scales this layout normally; never compensate its scale.
    private const float LayoutScale = 0.02f;

    [MenuItem("Tools/BAS VR/Normalize Assembly Menu Size")]
    public static void NormalizeSize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Normalize menu size outside Play Mode.");
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            contents.transform.localScale = Vector3.one;
            contents.transform.Find("Canvas").localScale = Vector3.one * LayoutScale;
            var canvas = contents.transform.Find("Canvas");
            var samples = contents.transform.Find("Samples") ?? canvas.Find("Samples");
            samples.localScale = samples.parent == canvas ? Vector3.one : Vector3.one * LayoutScale;
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var controller in root.GetComponentsInChildren<LeftHandMenu>(true))
        {
            var binding = new SerializedObject(controller);
            var menu = binding.FindProperty("sceneMenu").objectReferenceValue as GameObject;
            if (menu == null || menu.GetComponent<AssemblyMenuView>() == null) continue;
            Undo.RecordObject(menu.transform, "Normalize assembly menu size");
            menu.transform.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu.transform);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/BAS VR/Populate New Parts Menu")]
    public static void PopulateNewParts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure menu rows outside Play Mode.");
        var sources = AssetDatabase.FindAssets("t:Prefab", new[] { NewPartsMigration.Root })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(source => source.name, StringComparer.OrdinalIgnoreCase).ToArray();
        PartType[] categoryTypes = { PartType.Rays, PartType.Body, PartType.Propeller, PartType.Legs, PartType.Motor };
        var categorized = new List<GameObject>[6];
        for (int i = 0; i < categorized.Length; i++) categorized[i] = new List<GameObject>();
        foreach (var source in sources)
        {
            var link = source.GetComponent<PartViewConfigLink>();
            if (link == null || link.Config == null)
                throw new InvalidOperationException("Migrate the part before adding it to the menu: " + source.name);
            int category = Array.IndexOf(categoryTypes, link.Config.PartType);
            categorized[category < 0 ? 5 : category].Add(source);
        }
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = contents.GetComponent<AssemblyMenuView>();
            var data = new SerializedObject(view);
            var groups = data.FindProperty("groups");
            var rows = data.FindProperty("rows");
            rows.arraySize = groups.arraySize;
            var canvas = contents.GetComponentInChildren<Canvas>(true).transform;
            var samples = contents.transform.Find("Samples") ?? canvas.Find("Samples");
            samples.SetParent(canvas, false);
            samples.localScale = Vector3.one;
            var creation = contents.GetComponent<MenuPartCreationView>() ?? contents.AddComponent<MenuPartCreationView>();
            if (contents.GetComponent<CanvasGroup>() == null) contents.AddComponent<CanvasGroup>();
            var buttons = new List<Button>();
            var configs = new List<PartConfig>();
            var previewTransforms = new List<Transform>();
            for (int i = 0; i < groups.arraySize; i++)
            {
                var group = (GameObject)groups.GetArrayElementAtIndex(i).objectReferenceValue;
                group.transform.localPosition = new Vector3(0, 0, -100);
                var row = group.GetComponent<ObjectRowManager>();
                if (row == null) row = group.AddComponent<ObjectRowManager>();
                row.visibleObjectsCount = 5;
                row.spacing = 250;
                row.objectsContainer = group.transform;
                row.visibleObjectsParent = group.transform;
                row.circularScrolling = true;
                foreach (var old in row.allObjects)
                    if (old != null) UnityEngine.Object.DestroyImmediate(old);
                row.allObjects.Clear();
                foreach (var source in categorized[i])
                {
                        var slot = Rect(source.name, group.transform, new Vector2(210, 210), Vector2.zero);
                        var config = source.GetComponent<PartViewConfigLink>().Config;
                        slot.gameObject.AddComponent<PartViewConfigLink>().Config = config;
                        var rotation = new GameObject("PreviewRotation").transform;
                        rotation.SetParent(slot, false);
                        var visual = CopyPreviewVisual(source.transform, rotation, true);
                        visual.localRotation = Quaternion.Euler(60, 0, 0);
                        FitPreview(slot, visual, 170);
                        var image = slot.gameObject.AddComponent<Image>();
                        image.color = Color.white;
                        image.raycastTarget = true;
                        slot.GetComponent<CanvasRenderer>().cullTransparentMesh = false;
                        var button = slot.gameObject.AddComponent<Button>();
                        button.targetGraphic = image;
                        button.navigation = new Navigation { mode = Navigation.Mode.None };
                        var colors = button.colors;
                        colors.normalColor = Color.clear;
                        colors.highlightedColor = new Color(1, 1, 1, 0.08f);
                        colors.pressedColor = new Color(1, 1, 1, 0.15f);
                        colors.selectedColor = Color.clear;
                        button.colors = colors;
                        UnityEventTools.AddIntPersistentListener(button.onClick, creation.RequestCreatePart, buttons.Count);
                        buttons.Add(button);
                        configs.Add(config);
                        previewTransforms.Add(visual);
                        row.allObjects.Add(slot.gameObject);
                }
                row.RefreshLayout();
                group.SetActive(i == 0);
                rows.GetArrayElementAtIndex(i).objectReferenceValue = row;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var creationData = new SerializedObject(creation);
            var entries = creationData.FindProperty("entries");
            entries.arraySize = buttons.Count;
            for (int i = 0; i < buttons.Count; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("button").objectReferenceValue = buttons[i];
                entry.FindPropertyRelative("config").objectReferenceValue = configs[i];
                entry.FindPropertyRelative("previewTransform").objectReferenceValue = previewTransforms[i];
            }
            creationData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var menu in root.GetComponentsInChildren<AssemblyMenuView>(true))
            menu.SelectCategory(0);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Menu populated with " + sources.Length + " unique parts: " +
            string.Join(", ", categorized.Select((parts, i) => i + "=" + parts.Count)));
    }

    private static Transform CopyPreviewVisual(Transform source, Transform parent, bool isRoot)
    {
        if (source.GetComponentsInChildren<MeshRenderer>(true).Length == 0) return null;
        var copy = new GameObject(source.name).transform;
        copy.SetParent(parent, false);
        copy.localPosition = isRoot ? Vector3.zero : source.localPosition;
        copy.localRotation = isRoot ? Quaternion.identity : source.localRotation;
        copy.localScale = source.localScale;
        var filter = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        if (filter != null && renderer != null && filter.sharedMesh != null)
        {
            copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var previewRenderer = copy.gameObject.AddComponent<MeshRenderer>();
            previewRenderer.sharedMaterials = renderer.sharedMaterials;
            previewRenderer.enabled = renderer.enabled;
            previewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            previewRenderer.receiveShadows = false;
        }
        foreach (Transform child in source) CopyPreviewVisual(child, copy, false);
        copy.gameObject.SetActive(isRoot || source.gameObject.activeSelf);
        return copy;
    }

    private static void FitPreview(Transform slot, Transform visual, float size)
    {
        bool hasBounds = false;
        var bounds = new Bounds();
        foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            var local = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = slot.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(point);
            }
        }
        if (!hasBounds) throw new InvalidOperationException("Part has no preview mesh: " + slot.name);
        float factor = size / Mathf.Max(0.001f, bounds.size.x, bounds.size.y, bounds.size.z);
        visual.localScale *= factor;
        visual.localPosition = -bounds.center * factor;
    }

    [MenuItem("Tools/BAS VR/Inspect Assembly Menu")]
    public static void Inspect()
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var view in root.GetComponentsInChildren<AssemblyMenuView>(true))
        {
            Selection.activeGameObject = view.gameObject;
            var canvas = view.GetComponentInChildren<Canvas>(true);
            SceneView.lastActiveSceneView.LookAtDirect(canvas.transform.position,
                canvas.transform.rotation, 0.9f);
            Debug.Log("Assembly menu: canvas=" + canvas.renderMode + ", buttons=" +
                canvas.GetComponentsInChildren<Button>(true).Length);
            return;
        }
    }

    [MenuItem("Tools/BAS VR/Create Assembly Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity")
        {
            Debug.LogError("Open ConstructorScene before creating the assembly menu.");
            return;
        }
        LeftHandMenu controller = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentsInChildren<LeftHandMenu>(true);
            if (found.Length > 0) { controller = found[0]; break; }
        }
        if (controller == null) { Debug.LogError("LeftHandMenu not found."); return; }
        var binding = new SerializedObject(controller);
        if (binding.FindProperty("sceneMenu").objectReferenceValue != null)
        { Selection.activeObject = binding.FindProperty("sceneMenu").objectReferenceValue; return; }
        var anchor = (Transform)binding.FindProperty("menuTransform").objectReferenceValue;
        if (anchor == null) { Debug.LogError("LeftHandMenu menuTransform is missing."); return; }

        var menu = new GameObject("AssemblyMenu");
        Undo.RegisterCreatedObjectUndo(menu, "Create assembly menu");
        menu.transform.SetParent(anchor, false);
        var canvasRect = Rect("Canvas", menu.transform, new Vector2(1440, 650), Vector2.zero);
        canvasRect.localScale = Vector3.one * LayoutScale;
        var canvas = canvasRect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasRect.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        var samples = new GameObject("Samples").transform;
        samples.SetParent(canvasRect, false);
        var buttons = new Button[6];
        var groups = new GameObject[6];
        string[] labels = { "РАМА", "КОРПУС", "ВИНТ", "ШАССИ", "ДВИЖКИ", "ЭЛЕКТРОНИКА" };
        string[] names = { "Frame", "Body", "Propeller", "Chassis", "Motors", "Other" };
        Button previous = MakeButton(canvasRect, "Previous", "<", -630, 125);
        for (int i = 0; i < labels.Length; i++)
        {
            buttons[i] = MakeButton(canvasRect, names[i], labels[i], -450 + 180 * i, 165);
            groups[i] = new GameObject(names[i] + "Parts");
            groups[i].transform.SetParent(samples, false);
            // Reserved area above the category buttons; no demo parts or placeholder labels.
            groups[i].transform.localPosition = new Vector3(0, 15, 0);
            groups[i].SetActive(false);
        }
        Button next = MakeButton(canvasRect, "Next", ">", 630, 125);
        var view = menu.AddComponent<AssemblyMenuView>();
        var config = new SerializedObject(view);
        AssignArray(config, "categories", buttons);
        AssignArray(config, "groups", groups);
        config.FindProperty("previous").objectReferenceValue = previous;
        config.FindProperty("next").objectReferenceValue = next;
        config.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(menu, PrefabPath, InteractionMode.AutomatedAction);
        Undo.RecordObject(controller, "Assign assembly menu");
        binding.Update();
        binding.FindProperty("menuPrefab").objectReferenceValue = prefab;
        binding.FindProperty("sceneMenu").objectReferenceValue = menu;
        binding.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = menu;
        Debug.Log("AssemblyMenu created in ConstructorScene and saved as " + PrefabPath);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static Button MakeButton(Transform parent, string name, string label, float x, float width)
    {
        var rect = Rect(name, parent, new Vector2(width, 84), new Vector2(x, -250));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.30f, 0.30f, 0.28f, 0.72f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var textRect = Rect("Label", rect, new Vector2(width - 6, 80), Vector2.zero);
        var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.text = label;
        text.fontSize = label.Length == 1 ? 52 : 29;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private static void AssignArray<T>(SerializedObject target, string name, T[] values) where T : UnityEngine.Object
    {
        var array = target.FindProperty(name);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
#endif
