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
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class MotorCoverCatalogSetup
{
    public const string Prefabs = "Assets/_GAME/Prefabs/NewParts/MotorCover/";
    public const string Configs = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/MotorCover/";
    private const string Model = "Assets/_GAME/3D_Models/DronesParts/MotorCovers/Motor_Covers.fbx";

    [MenuItem("Tools/BAS VR/Setup Motor Cover Catalog")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Exit Play Mode and Prefab Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity") throw new InvalidOperationException("Open ConstructorScene.");
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Configs);
        AssetDatabase.Refresh();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if (source == null) throw new InvalidOperationException("Motor cover FBX missing.");
        var configs = new List<PartConfig>();
        foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true)) configs.Add(CreateCover(filter));
        AddArmSockets();
        foreach (var config in configs) ArmCoverPrototypeSetup.AddMenuEntry(config);
        ExtendPanel(scene);
        AssetDatabase.SaveAssets();
        NewPartsMigration.RegisterInScenes(configs);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Motor covers: {configs.Count} prefabs/configs, arm sockets, Bodywork menu and uniform XYZ handle installed.");
    }

    private static PartConfig CreateCover(MeshFilter filter)
    {
        string name = filter.name;
        string configPath = Configs + name + "Config.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PartConfig>(configPath);
        if (existing != null && existing.Prefab != null) return existing;
        // Bake only the imported rotation/scale. Export layout translation is not part geometry.
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
        mesh.name = name + "_Mesh";
        var matrix = Matrix4x4.TRS(Vector3.zero, filter.transform.localRotation, filter.transform.localScale);
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        mesh.vertices = vertices;
        var normals = mesh.normals;
        var normalMatrix = matrix.inverse.transpose;
        for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
        string meshPath = Prefabs + mesh.name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); }
        var config = existing ?? ScriptableObject.CreateInstance<PartConfig>();
        if (existing == null)
        {
            config.name = name + "Config";
            config.PartId = "newparts/MotorCover/" + name;
            config.PrefabAddress = "Parts/MotorCover/" + name;
            config.PartType = PartType.MotorCover;
            config.Mass = 0.006f; // Initial estimate in kg, adjustable per design.
            AssetDatabase.CreateAsset(config, configPath);
        }
        config.LocalCenterOfMass = saved.bounds.center;
        var root = PrefabUtility.LoadPrefabContents(ArmCoverPrototypeSetup.PrefabPath);
        string prefabPath = Prefabs + name + ".prefab";
        try
        {
            root.name = name;
            root.transform.localScale = Vector3.one;
            var model = root.GetComponentInChildren<MeshFilter>(true);
            model.sharedMesh = saved;
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            model.GetComponent<MeshRenderer>().sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            root.GetComponent<ArmCoverGeometry>().LocalBounds = saved.bounds;
            var box = root.GetComponent<BoxCollider>();
            box.center = saved.bounds.center;
            box.size = Vector3.Max(saved.bounds.size, Vector3.one * 0.002f);
            root.GetComponent<Rigidbody>().mass = config.Mass;
            root.GetComponent<PartViewConfigLink>().Config = config;
            root.GetComponentInChildren<CenterOfMassMarker>(true).transform.localPosition = config.LocalCenterOfMass;
            config.Prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var templateEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(ArmCoverPrototypeSetup.PrefabPath));
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), templateEntry.parentGroup).SetAddress(config.PrefabAddress);
        EditorUtility.SetDirty(config);
        EditorUtility.SetDirty(settings);
        return config;
    }

    private static void AddArmSockets()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_GAME" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var config = prefab.GetComponent<PartViewConfigLink>()?.Config;
            if (config == null || config.PartType != PartType.Arm) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var existing = root.GetComponentsInChildren<SocketView>(true).FirstOrDefault(s => s.AllowedTypes.Contains(PartType.MotorCover));
                if (existing != null)
                {
                    OrientOpeningAlongArm(root.transform, existing);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    continue;
                }
                var motor = root.GetComponentsInChildren<SocketView>(true).FirstOrDefault(s => s.AllowedTypes.Contains(PartType.Motor));
                if (motor == null) throw new InvalidOperationException("Arm has no motor mount: " + path);
                var copy = UnityEngine.Object.Instantiate(motor.gameObject, root.transform, false);
                copy.name = "MotorCoverSocket";
                copy.transform.localPosition = root.transform.InverseTransformPoint(motor.AttachPoint.position);
                copy.transform.localRotation = Quaternion.Inverse(root.transform.rotation) * motor.AttachPoint.rotation;
                copy.transform.localScale = Vector3.one;
                var socket = copy.GetComponent<SocketView>();
                var attach = socket.AttachPoint;
                attach.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var sphere = copy.GetComponent<SphereCollider>();
                sphere.center = Vector3.zero;
                sphere.radius = .025f;
                sphere.isTrigger = true;
                sphere.enabled = true;
                var interactor = copy.GetComponent<HoverOnlySocketInteractor>();
                interactor.attachTransform = attach;
                interactor.showInteractableHoverMeshes = false;
                var xr = new SerializedObject(interactor);
                xr.FindProperty("_mountProximityCollider").objectReferenceValue = sphere;
                xr.FindProperty("_mountCompetitors").arraySize = 0;
                xr.ApplyModifiedPropertiesWithoutUndo();
                var data = new SerializedObject(socket);
                data.FindProperty("_socketId").stringValue = "mount/MotorCover/0";
                var allowed = data.FindProperty("AllowedTypes");
                allowed.arraySize = 1;
                allowed.GetArrayElementAtIndex(0).intValue = (int)PartType.MotorCover;
                data.FindProperty("_previewAnchor").objectReferenceValue = attach;
                data.ApplyModifiedPropertiesWithoutUndo();
                copy.SetActive(true);
                OrientOpeningAlongArm(root.transform, socket);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Motor cover mount added: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    // All ten meshes have their open-sector bisector at -45 degrees in the local XZ plane.
    // Aim it inward, from the motor centre towards the root of this particular arm.
    private static void OrientOpeningAlongArm(Transform arm, SocketView socket)
    {
        var inward = -arm.InverseTransformPoint(socket.transform.position);
        inward.y = 0;
        if (inward.sqrMagnitude < .000001f) inward = Vector3.left;
        socket.transform.localRotation = Quaternion.FromToRotation(new Vector3(1, 0, -1).normalized, inward.normalized);
        socket.AttachPoint.localRotation = Quaternion.identity;
    }

    [MenuItem("Tools/BAS VR/Update Motor Cover Fitting")]
    public static void UpdateFitting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Edit Mode required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity") throw new InvalidOperationException("Open ConstructorScene.");
        AddArmSockets();
        ExtendPanel(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ExtendPanel(UnityEngine.SceneManagement.Scene scene)
    {
        var owner = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ArmCoverAdjustmentView>(true)).Single();
        var data = new SerializedObject(owner);
        var handles = data.FindProperty("handles");
        var gizmo = (Transform)data.FindProperty("gizmo").objectReferenceValue;
        if (!gizmo.GetComponentsInChildren<ArmCoverScaleHandle>(true).Any(h => h.Axis == 4))
        {
            var template = (ArmCoverScaleHandle)handles.GetArrayElementAtIndex(0).objectReferenceValue;
            var handle = UnityEngine.Object.Instantiate(template, gizmo, false);
            Undo.RegisterCreatedObjectUndo(handle.gameObject, "Create uniform cover scale handle");
            handle.name = "Uniform_XYZ";
            var binding = new SerializedObject(handle);
            binding.FindProperty("axis").intValue = 4;
            binding.ApplyModifiedPropertiesWithoutUndo();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Prefabs + "UniformScaleHandle.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", new Color(.95f, .22f, .85f));
                AssetDatabase.CreateAsset(mat, Prefabs + "UniformScaleHandle.mat");
            }
            var renderer = handle.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            handles.arraySize++;
            handles.GetArrayElementAtIndex(handles.arraySize - 1).objectReferenceValue = handle;
            data.ApplyModifiedProperties();
        }
        var fitting = (GameObject)data.FindProperty("fittingContent").objectReferenceValue;
        var axisMethods = new[] { "LengthMinus", "LengthPlus", "WidthMinus", "WidthPlus", "ToggleGizmo" };
        var axisButtons = fitting.GetComponentsInChildren<Button>(true).Where(b => b.onClick.GetPersistentEventCount() > 0 &&
            axisMethods.Contains(b.onClick.GetPersistentMethodName(0))).ToArray();
        var axisControls = data.FindProperty("axisControls");
        axisControls.arraySize = axisButtons.Length;
        for (int i = 0; i < axisButtons.Length; i++) axisControls.GetArrayElementAtIndex(i).objectReferenceValue = axisButtons[i].gameObject;
        var reset = fitting.GetComponentsInChildren<Button>(true).First(b => b.onClick.GetPersistentEventCount() > 0 &&
            b.onClick.GetPersistentMethodName(0) == "ResetScale");
        var motorButtons = data.FindProperty("motorScaleControls");
        motorButtons.arraySize = 2;
        for (int i = 0; i < 2; i++)
        {
            string name = i == 0 ? "MotorUniformMinus" : "MotorUniformPlus";
            var existingButton = fitting.transform.Find(name);
            var button = existingButton != null ? existingButton.GetComponent<Button>() : UnityEngine.Object.Instantiate(reset, reset.transform.parent, false);
            button.name = name;
            ((RectTransform)button.transform).anchoredPosition = new Vector2(i == 0 ? 3.3f : 9.6f, -2.1f);
            while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
            if (i == 0) UnityEventTools.AddPersistentListener(button.onClick, owner.UniformMinus);
            else UnityEventTools.AddPersistentListener(button.onClick, owner.UniformPlus);
            button.GetComponentInChildren<TMP_Text>(true).text = i == 0 ? "Масштаб −5%" : "Масштаб +5%";
            motorButtons.GetArrayElementAtIndex(i).objectReferenceValue = button.gameObject;
        }
        data.FindProperty("fittingHint").objectReferenceValue = fitting.GetComponentsInChildren<TMP_Text>(true)
            .First(t => t.text.StartsWith("Красная") || t.text.StartsWith("Ручки: красная") || t.text.StartsWith("Кожух мотора:"));
        data.ApplyModifiedProperties();
        var heightControls = data.FindProperty("heightButtons");
        var heightPair = fitting.GetComponentsInChildren<Button>(true).Where(b => b.onClick.GetPersistentEventCount() > 0 &&
            (b.onClick.GetPersistentMethodName(0) == "HeightMinus" || b.onClick.GetPersistentMethodName(0) == "HeightPlus")).ToArray();
        heightControls.arraySize = heightPair.Length;
        for (int i = 0; i < heightPair.Length; i++) heightControls.GetArrayElementAtIndex(i).objectReferenceValue = heightPair[i];
        data.ApplyModifiedProperties();
        if (fitting.transform.Find("UniformScaleButton") == null)
        {
            var template = fitting.GetComponentsInChildren<Button>(true).First(b => b.onClick.GetPersistentMethodName(0) == "ToggleGizmo");
            var button = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
            Undo.RegisterCreatedObjectUndo(button.gameObject, "Create uniform cover scale button");
            button.name = "UniformScaleButton";
            ((RectTransform)button.transform).anchoredPosition = new Vector2(15.9f, -4.3f);
            while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
            UnityEventTools.AddPersistentListener(button.onClick, owner.ToggleUniformGizmo);
            button.GetComponentInChildren<TMP_Text>(true).text = "Масштаб XYZ";
        }
        foreach (var label in fitting.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.text.StartsWith("Накладка луча")) { Undo.RecordObject(label, "Cover fitting title"); label.text = "Кожух луча / мотора • размеры ±5%"; }
            if (label.text.StartsWith("Ручки: красная"))
            {
                Undo.RecordObject(label, "Cover handle description");
                label.text = "Красная — длина; зелёная — высота; синяя — ширина; жёлтая — вдоль луча. XYZ — все оси пропорционально.";
            }
        }
        LayoutPanel(owner, fitting.transform);
    }

    private static void LayoutPanel(ArmCoverAdjustmentView owner, Transform fitting)
    {
        // Three columns with fixed gaps. Shared rows use mutually exclusive arm/motor controls.
        foreach (var button in fitting.GetComponentsInChildren<Button>(true))
        {
            if (button.onClick.GetPersistentEventCount() == 0) continue;
            string method = button.onClick.GetPersistentMethodName(0);
            Vector2 position;
            switch (method)
            {
                case "LengthMinus": case "UniformMinus": position = new Vector2(3.3f, -2.1f); break;
                case "LengthPlus": case "UniformPlus": position = new Vector2(9.6f, -2.1f); break;
                case "ResetScale": position = new Vector2(15.9f, -2.1f); break;
                case "HeightMinus": position = new Vector2(3.3f, -3.35f); break;
                case "HeightPlus": position = new Vector2(9.6f, -3.35f); break;
                case "ToggleUniformGizmo": position = new Vector2(15.9f, -3.35f); break;
                case "WidthMinus": position = new Vector2(3.3f, -4.6f); break;
                case "WidthPlus": position = new Vector2(9.6f, -4.6f); break;
                case "ToggleGizmo": position = new Vector2(15.9f, -4.6f); break;
                case "AlongMinus": position = new Vector2(3.3f, -5.85f); break;
                case "AlongPlus": position = new Vector2(9.6f, -5.85f); break;
                case "ResetAlong": position = new Vector2(15.9f, -5.85f); break;
                default: continue;
            }
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Arrange cover fitting controls");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            // Button positions denote centres; use centred pivot with top-left anchors.
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(5.8f, 1.05f);
        }
        var data = new SerializedObject(owner);
        var hint = (TMP_Text)data.FindProperty("fittingHint").objectReferenceValue;
        var hintRect = (RectTransform)hint.transform;
        Undo.RecordObject(hintRect, "Separate fitting hint from buttons");
        hintRect.anchoredPosition = new Vector2(9.6f, -7.05f);
        hintRect.sizeDelta = new Vector2(18, 1.0f);
        var fittingRect = (RectTransform)fitting;
        Undo.RecordObject(fittingRect, "Size cover fitting background");
        fittingRect.sizeDelta = new Vector2(19.2f, 7.75f);
        var wrapper = (GameObject)data.FindProperty("controls").objectReferenceValue;
        Undo.RecordObject(wrapper.transform, "Size cover fitting controls");
        ((RectTransform)wrapper.transform).sizeDelta = new Vector2(19.2f, 9.0f);
        foreach (var button in wrapper.GetComponentsInChildren<Button>(true))
            if (button.onClick.GetPersistentEventCount() > 0 && button.onClick.GetPersistentMethodName(0) == "ToggleProperties")
            {
                Undo.RecordObject(button.transform, "Separate material toggle");
                ((RectTransform)button.transform).anchoredPosition = new Vector2(15.9f, -8.35f);
            }
    }
}
#endif
