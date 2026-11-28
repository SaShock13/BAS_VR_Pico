#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ArmCoverPrototypeSetup
{
    public const string PrefabPath = "Assets/_GAME/Prefabs/NewParts/ArmCover/R2_Arm_Cover_1.prefab";
    public const string ConfigPath = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/ArmCover/R2_Arm_Cover_1Config.asset";
    public const string ArmPath = "Assets/_GAME/Prefabs/NewParts/Arm/R2_Frame_Arm_14in.prefab";
    private const string MenuPath = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
    private const string MeshPath = "Assets/_GAME/Prefabs/NewParts/ArmCover/R2_Arm_Cover_1_Mesh.asset";
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");

    [MenuItem("Tools/BAS VR/Setup R2 Arm Cover Prototype")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity") throw new InvalidOperationException("Open ConstructorScene.");
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        AssetDatabase.Refresh();
        var config = BuildCover();
        AddSocket();
        AddMenuEntry(config);
        // Garage and flight must also resolve the cover when loading an assembly save.
        NewPartsMigration.RegisterInScenes(new List<PartConfig> { config });
        BuildPanel();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("R2 cover prototype installed: menu, 14in arm socket, dimension/offset panel and four XR handles.");
    }

    private static PartConfig BuildCover()
    {
        var sourcePath = AssetDatabase.FindAssets("Arm_Cover_1").Select(AssetDatabase.GUIDToAssetPath)
            .First(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase));
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        var filter = source.GetComponentInChildren<MeshFilter>();
        // The FBX includes Blender's object translation. Drop this translation and rotate its arm
        // direction to +X, matching the existing R2 arm prefab. Bake the import scale into the mesh.
        var matrix = Matrix4x4.TRS(Vector3.zero,
            Quaternion.Euler(0, -45, 0) * filter.transform.localRotation, filter.transform.localScale);
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
        mesh.name = "R2_Arm_Cover_1_Mesh";
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        mesh.vertices = vertices;
        var normals = mesh.normals;
        var normalMatrix = matrix.inverse.transpose;
        for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, MeshPath); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); }

        var config = AssetDatabase.LoadAssetAtPath<PartConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PartConfig>();
            config.PartId = "newparts/ArmCover/R2_Arm_Cover_1";
            config.PrefabAddress = "Parts/ArmCover/R2_Arm_Cover_1";
            config.PartType = PartType.ArmCover;
            config.Mass = 0.008f; // Prototype estimate, kg.
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.LocalCenterOfMass = saved.bounds.center;
        var root = new GameObject("R2_Arm_Cover_1");
        try
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/Prefabs/Parts/Controller1.prefab");
            root.layer = template.layer;
            var model = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(root.transform, false);
            model.layer = root.layer;
            model.GetComponent<MeshFilter>().sharedMesh = saved;
            model.GetComponent<MeshRenderer>().sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            root.AddComponent<ArmCoverGeometry>().LocalBounds = saved.bounds;
            var collider = root.AddComponent<BoxCollider>(); collider.center = saved.bounds.center; collider.size = saved.bounds.size;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; body.mass = config.Mass;
            var grab = root.AddComponent<XRGrabInteractable>();
            EditorUtility.CopySerialized(template.GetComponent<XRGrabInteractable>(), grab);
            grab.attachTransform = null; grab.secondaryAttachTransform = null;
            grab.colliders.Clear(); grab.colliders.Add(collider);
            grab.interactionLayers = 1; grab.trackScale = false; grab.throwOnDetach = false; grab.retainTransformParent = false;
            root.AddComponent<PartViewConfigLink>().Config = config;
            var com = new GameObject("CenterOfMass").AddComponent<CenterOfMassMarker>();
            com.transform.SetParent(root.transform, false); com.transform.localPosition = config.LocalCenterOfMass;
            config.Prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("New Parts") ?? settings.DefaultGroup;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PrefabPath), group).SetAddress(config.PrefabAddress);
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(config);
        return config;
    }

    private static void AddSocket()
    {
        var root = PrefabUtility.LoadPrefabContents(ArmPath);
        try
        {
            var socket = root.transform.Find("ArmCoverSocket");
            if (socket != null) return; // Preserve the authored socket, including user-tuned collider dimensions.
            if (socket == null) { socket = new GameObject("ArmCoverSocket").transform; socket.SetParent(root.transform, false); }
            socket.localPosition = Vector3.zero; socket.localRotation = Quaternion.identity; socket.localScale = Vector3.one;
            socket.gameObject.layer = root.layer;
            var attach = socket.Find("NewPartsAttachPoint");
            if (attach == null) { attach = new GameObject("NewPartsAttachPoint").transform; attach.SetParent(socket, false); }
            var region = socket.GetComponent<SphereCollider>();
            if (region == null) region = socket.gameObject.AddComponent<SphereCollider>();
            region.center = new Vector3(0.065f, 0, 0); region.radius = 0.03f; region.isTrigger = true;
            var xr = socket.GetComponent<HoverOnlySocketInteractor>();
            if (xr == null) xr = socket.gameObject.AddComponent<HoverOnlySocketInteractor>();
            xr.interactionLayers = 1; xr.attachTransform = attach; xr.showInteractableHoverMeshes = false;
            var view = socket.GetComponent<SocketView>();
            if (view == null) view = socket.gameObject.AddComponent<SocketView>();
            var so = new SerializedObject(view);
            so.FindProperty("_socketId").stringValue = "mount/ArmCover/0";
            var allowed = so.FindProperty("AllowedTypes"); allowed.arraySize = 1; allowed.GetArrayElementAtIndex(0).intValue = (int)PartType.ArmCover;
            so.FindProperty("AttachPoint").objectReferenceValue = attach;
            so.FindProperty("_previewAnchor").objectReferenceValue = attach;
            so.FindProperty("_socketInteractor").objectReferenceValue = xr;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ArmPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    internal static void AddMenuEntry(PartConfig config, string rowName = null)
    {
        var root = PrefabUtility.LoadPrefabContents(MenuPath);
        try
        {
            var publisher = root.GetComponent<MenuPartCreationView>();
            var so = new SerializedObject(publisher); var entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("config").objectReferenceValue == config) return;
            var row = root.GetComponentsInChildren<ObjectRowManager>(true).FirstOrDefault(r => r.name == (rowName ?? "BodyworkParts"))
                ?? root.GetComponentsInChildren<ObjectRowManager>(true).First(r => r.name == "OtherParts");
            var slot = Rect(config.Prefab.name, row.transform, new Vector2(210, 210), Vector2.zero);
            slot.gameObject.AddComponent<PartViewConfigLink>().Config = config;
            var image = slot.gameObject.AddComponent<Image>(); image.color = Color.white;
            slot.GetComponent<CanvasRenderer>().cullTransparentMesh = false;
            var button = slot.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.clear; colors.selectedColor = Color.clear;
            colors.highlightedColor = new Color(1,1,1,.08f); colors.pressedColor = new Color(1,1,1,.15f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var rotation = new GameObject("PreviewRotation").transform; rotation.SetParent(slot, false);
            var visual = new GameObject("CoverPreview", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            visual.SetParent(rotation, false);
            var model = config.Prefab.GetComponentInChildren<MeshFilter>();
            visual.GetComponent<MeshFilter>().sharedMesh = model.sharedMesh;
            var renderer = visual.GetComponent<MeshRenderer>(); renderer.sharedMaterials = model.GetComponent<MeshRenderer>().sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var bounds = model.sharedMesh.bounds;
            float factor = 170f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            visual.localScale = Vector3.one * factor; visual.localPosition = -bounds.center * factor;
            rotation.localRotation = Quaternion.Euler(60, 0, 0);
            int index = entries.arraySize; entries.arraySize++;
            var e = entries.GetArrayElementAtIndex(index);
            e.FindPropertyRelative("button").objectReferenceValue = button;
            e.FindPropertyRelative("config").objectReferenceValue = config;
            e.FindPropertyRelative("previewTransform").objectReferenceValue = visual;
            UnityEventTools.AddIntPersistentListener(button.onClick, publisher.RequestCreatePart, index);
            row.allObjects.Add(slot.gameObject); row.RefreshLayout();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, MenuPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildPanel()
    {
        var panel = UnityEngine.Object.FindFirstObjectByType<PartPanelUI>(FindObjectsInactive.Include);
        if (panel.GetComponent<ArmCoverAdjustmentView>() != null) return;
        var owner = Undo.AddComponent<ArmCoverAdjustmentView>(panel.gameObject);
        var panelObject = (GameObject)new SerializedObject(panel).FindProperty("panel").objectReferenceValue;
        var wrapper = Rect("ArmCoverAdjustment", panelObject.transform, new Vector2(19.2f, 7.85f), new Vector2(.4f, -.1f));
        wrapper.anchorMin = wrapper.anchorMax = wrapper.pivot = new Vector2(0, 1);
        var group = Rect("FittingContent", wrapper, new Vector2(19.2f, 7f), Vector2.zero);
        group.anchorMin = group.anchorMax = group.pivot = new Vector2(0, 1);
        group.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .06f, 1);
        Label(group, "Накладка луча • размеры ±5%", new Vector2(9.6f, -.45f), new Vector2(18, .7f), .5f);
        var dimensions = Label(group, "Размеры", new Vector2(9.6f, -1.15f), new Vector2(18, .7f), .42f);
        Button(group, "Длина −", new Vector2(3.3f, -2.1f), owner.LengthMinus);
        Button(group, "Длина +", new Vector2(9.6f, -2.1f), owner.LengthPlus);
        Button(group, "Высота −", new Vector2(3.3f, -3.2f), owner.HeightMinus);
        Button(group, "Высота +", new Vector2(9.6f, -3.2f), owner.HeightPlus);
        Button(group, "Ширина −", new Vector2(3.3f, -4.3f), owner.WidthMinus);
        Button(group, "Ширина +", new Vector2(9.6f, -4.3f), owner.WidthPlus);
        var alongMinus = Button(group, "Вдоль −1 мм", new Vector2(3.3f, -5.4f), owner.AlongMinus);
        var alongPlus = Button(group, "Вдоль +1 мм", new Vector2(9.6f, -5.4f), owner.AlongPlus);
        var alongReset = Button(group, "Сброс сдвига", new Vector2(15.9f, -5.4f), owner.ResetAlong);
        alongMinus.interactable = alongPlus.interactable = alongReset.interactable = false;
        Button(group, "Сброс размера", new Vector2(15.9f, -2.1f), owner.ResetScale);
        var toggle = Button(group, "Ручки: выкл.", new Vector2(15.9f, -3.2f), owner.ToggleGizmo);
        Button(wrapper, "Размер / Материал", new Vector2(15.9f, -7.25f), owner.ToggleProperties);
        Label(group, "Ручки: красная — длина; зелёная — высота; синяя — ширина; жёлтая — вдоль луча.",
            new Vector2(9.6f, -6.5f), new Vector2(18, 1.1f), .38f);
        var gizmo = new GameObject("ArmCoverRuntimeGizmo");
        Undo.RegisterCreatedObjectUndo(gizmo, "Create arm cover handles");
        var handles = new ArmCoverScaleHandle[4];
        Color[] colors = { new Color(.95f,.12f,.08f), new Color(.15f,.9f,.25f), new Color(.15f,.4f,1), new Color(1,.75f,.08f) };
        string[] names = { "Length_X", "Height_Y", "Width_Z", "Move_Along_Arm" };
        for (int i = 0; i < 4; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = names[i]; cube.layer = configLayer();
            if (i == 3) cube.GetComponent<MeshFilter>().sharedMesh = ArmCoverCatalogSetup.GetArrowMesh();
            cube.transform.SetParent(gizmo.transform, false); cube.transform.localScale = Vector3.one * .018f;
            var matPath = "Assets/_GAME/Prefabs/NewParts/ArmCover/Handle_" + i + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", colors[i]); AssetDatabase.CreateAsset(mat, matPath); }
            var renderer = cube.GetComponent<Renderer>(); renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            cube.GetComponent<BoxCollider>().size = i == 3 ? new Vector3(2.4f, 1.8f, 1.7f) : Vector3.one * 1.7f;
            handles[i] = cube.AddComponent<ArmCoverScaleHandle>();
            handles[i].interactionLayers = 1;
            handles[i].colliders.Clear(); handles[i].colliders.Add(cube.GetComponent<BoxCollider>());
            var binding = new SerializedObject(handles[i]); binding.FindProperty("axis").intValue = i; binding.ApplyModifiedPropertiesWithoutUndo();
        }
        var data = new SerializedObject(owner);
        data.FindProperty("controls").objectReferenceValue = wrapper.gameObject;
        data.FindProperty("fittingContent").objectReferenceValue = group.gameObject;
        var legacyAdjustment = panelObject.transform.Find("AdjustmentPanel");
        var sections = data.FindProperty("standardSections"); sections.arraySize = legacyAdjustment != null ? 2 : 1;
        sections.GetArrayElementAtIndex(0).objectReferenceValue = panelObject.transform.Find("InfoPanel").gameObject;
        if (legacyAdjustment != null) sections.GetArrayElementAtIndex(1).objectReferenceValue = legacyAdjustment.gameObject;
        data.FindProperty("dimensions").objectReferenceValue = dimensions;
        data.FindProperty("gizmoLabel").objectReferenceValue = toggle.GetComponentInChildren<TMP_Text>();
        data.FindProperty("gizmo").objectReferenceValue = gizmo.transform;
        var array = data.FindProperty("handles"); array.arraySize = handles.Length;
        for (int i = 0; i < handles.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = handles[i];
        var buttons = data.FindProperty("alongButtons"); buttons.arraySize = 3;
        buttons.GetArrayElementAtIndex(0).objectReferenceValue = alongMinus;
        buttons.GetArrayElementAtIndex(1).objectReferenceValue = alongPlus;
        buttons.GetArrayElementAtIndex(2).objectReferenceValue = alongReset;
        data.ApplyModifiedProperties();
        wrapper.gameObject.SetActive(false); gizmo.SetActive(false);
    }

    private static int configLayer() => AssetDatabase.LoadAssetAtPath<GameObject>(ArmPath).layer;

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var t = (RectTransform)go.transform; t.SetParent(parent, false); t.sizeDelta = size;
        t.anchorMin = t.anchorMax = new Vector2(.5f,.5f); t.anchoredPosition = position; return t;
    }
    private static TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = Rect(text, parent, size, position);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.anchoredPosition = position;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font; label.text = text; label.fontSize = fontSize; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string text, Vector2 position, UnityAction action)
    {
        var rect = Rect(text, parent, new Vector2(5.8f, 1.05f), position);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.anchoredPosition = position;
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f,.16f,.2f,1);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(button.onClick, action);
        var label = Label(rect, text, Vector2.zero, new Vector2(5.6f,.95f), .43f);
        var lr = (RectTransform)label.transform; lr.anchorMin = lr.anchorMax = new Vector2(.5f,.5f); lr.anchoredPosition = Vector2.zero;
        return button;
    }
}
#endif
