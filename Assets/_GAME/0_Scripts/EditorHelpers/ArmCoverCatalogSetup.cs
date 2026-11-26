#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Additive authoring: retain the first cover and the user's tuned 14in socket as templates.
public static class ArmCoverCatalogSetup
{
    private const string Models = "Assets/_GAME/3D_Models/DronesParts/Drones/R2(MARK4, 5 дюймов, )/CoversNBodies";
    private const string Prefabs = "Assets/_GAME/Prefabs/NewParts/ArmCover/";
    private const string Configs = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/ArmCover/";
    private const string Menu = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
    private const string ArrowPath = Prefabs + "MoveAlongArrow.asset";

    [MenuItem("Tools/BAS VR/Expand Arm Cover Catalog")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Exit Play Mode and Prefab Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity") throw new InvalidOperationException("Open ConstructorScene.");
        var configs = new List<PartConfig> { AssetDatabase.LoadAssetAtPath<PartConfig>(ArmCoverPrototypeSetup.ConfigPath) };
        for (int i = 2; i <= 5; i++) configs.Add(CreateCover(i));
        EnsureCategory();
        foreach (var config in configs) ArmCoverPrototypeSetup.AddMenuEntry(config);
        MoveCoversToCategory();
        CopySockets();
        ReplaceMovementHandle();
        AssetDatabase.SaveAssets();
        NewPartsMigration.RegisterInScenes(configs);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Arm covers: five catalog entries, Bodywork category, authored arm sockets and arrow handle installed.");
    }

    private static PartConfig CreateCover(int number)
    {
        string name = "R2_Arm_Cover_" + number;
        string prefabPath = Prefabs + name + ".prefab";
        string configPath = Configs + name + "Config.asset";
        // Re-running the authoring command must not overwrite subsequent manual tuning.
        var existing = AssetDatabase.LoadAssetAtPath<PartConfig>(configPath);
        if (existing != null && existing.Prefab != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/Arm_Cover_" + number + ".fbx");
        if (source == null) throw new InvalidOperationException("Cover model missing: " + number);
        var filters = source.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length != 1) throw new InvalidOperationException("Expected one mesh in " + source.name);
        var filter = filters[0];
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
        mesh.name = name + "_Mesh";
        var imported = Matrix4x4.TRS(Vector3.zero, filter.transform.localRotation, filter.transform.localScale);
        var center = imported.MultiplyPoint3x4(mesh.bounds.center);
        // The exports face opposite arm diagonals. Orient the radial direction to the arm's +X.
        float yaw = center.x < 0 ? 135f : -45f;
        var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, yaw, 0)) * imported;
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        mesh.vertices = vertices;
        var normalMatrix = matrix.inverse.transpose;
        var normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
        string meshPath = Prefabs + mesh.name + ".asset";
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
        else { EditorUtility.CopySerialized(mesh, savedMesh); UnityEngine.Object.DestroyImmediate(mesh); }
        var template = AssetDatabase.LoadAssetAtPath<PartConfig>(ArmCoverPrototypeSetup.ConfigPath);
        var config = existing;
        if (config == null)
        {
            config = UnityEngine.Object.Instantiate(template);
            config.name = name + "Config";
            config.PartId = "newparts/ArmCover/" + name;
            config.PrefabAddress = "Parts/ArmCover/" + name;
            config.LegacyPartIds = Array.Empty<string>();
            AssetDatabase.CreateAsset(config, configPath);
        }
        config.LocalCenterOfMass = savedMesh.bounds.center;
        var root = PrefabUtility.LoadPrefabContents(ArmCoverPrototypeSetup.PrefabPath);
        try
        {
            root.name = name;
            var model = root.GetComponentInChildren<MeshFilter>(true);
            model.sharedMesh = savedMesh;
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            model.GetComponent<MeshRenderer>().sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            root.GetComponent<ArmCoverGeometry>().LocalBounds = savedMesh.bounds;
            var collider = root.GetComponent<BoxCollider>();
            collider.center = savedMesh.bounds.center; collider.size = savedMesh.bounds.size;
            root.GetComponent<PartViewConfigLink>().Config = config;
            root.GetComponentInChildren<CenterOfMassMarker>(true).transform.localPosition = config.LocalCenterOfMass;
            config.Prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(ArmCoverPrototypeSetup.PrefabPath));
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), entry.parentGroup).SetAddress(config.PrefabAddress);
        EditorUtility.SetDirty(config); EditorUtility.SetDirty(settings);
        return config;
    }

    private static void EnsureCategory()
    {
        var root = PrefabUtility.LoadPrefabContents(Menu);
        try
        {
            var menu = root.GetComponent<AssemblyMenuView>();
            var so = new SerializedObject(menu);
            var categories = so.FindProperty("categories");
            var groups = so.FindProperty("groups");
            var rows = so.FindProperty("rows");
            if (root.GetComponentsInChildren<ObjectRowManager>(true).All(r => r.name != "BodyworkParts"))
            {
                int index = categories.arraySize;
                if (index != 6) throw new InvalidOperationException("Unexpected menu category layout.");
                var template = (Button)categories.GetArrayElementAtIndex(5).objectReferenceValue;
                var category = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                category.name = "Bodywork";
                while (category.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(category.onClick, 0);
                UnityEventTools.AddIntPersistentListener(category.onClick, menu.SelectCategory, index);
                category.GetComponentInChildren<TMP_Text>(true).text = "Корпусные детали";
                var previousRow = (ObjectRowManager)rows.GetArrayElementAtIndex(5).objectReferenceValue;
                var group = new GameObject("BodyworkParts"); group.transform.SetParent(previousRow.transform.parent, false);
                group.transform.localPosition = previousRow.transform.localPosition;
                var row = group.AddComponent<ObjectRowManager>(); EditorUtility.CopySerialized(previousRow, row);
                row.allObjects = new List<GameObject>(); row.objectsContainer = row.visibleObjectsParent = group.transform;
                group.SetActive(false);
                categories.arraySize++; groups.arraySize++; rows.arraySize++;
                categories.GetArrayElementAtIndex(index).objectReferenceValue = category;
                groups.GetArrayElementAtIndex(index).objectReferenceValue = group;
                rows.GetArrayElementAtIndex(index).objectReferenceValue = row;
            }
            for (int i = 0; i < categories.arraySize; i++)
            {
                var button = (Button)categories.GetArrayElementAtIndex(i).objectReferenceValue;
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(150, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(-480 + 160 * i, rect.anchoredPosition.y);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 24;
                var lr = (RectTransform)label.transform;
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = new Vector2(5, 4); lr.offsetMax = new Vector2(-5, -4);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Menu);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void MoveCoversToCategory()
    {
        var root = PrefabUtility.LoadPrefabContents(Menu);
        try
        {
            var rows = root.GetComponentsInChildren<ObjectRowManager>(true);
            var target = rows.Single(r => r.name == "BodyworkParts");
            var covers = root.GetComponentsInChildren<PartViewConfigLink>(true)
                .Where(l => l.Config != null && l.Config.PartType == PartType.ArmCover).ToArray();
            foreach (var cover in covers)
            {
                foreach (var row in rows) row.allObjects.Remove(cover.gameObject);
                target.allObjects.Add(cover.gameObject);
            }
            target.allObjects.Sort((a,b) => string.CompareOrdinal(a.name, b.name));
            foreach (var row in rows) row.RefreshLayout();
            PrefabUtility.SaveAsPrefabAsset(root, Menu);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void CopySockets()
    {
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(ArmCoverPrototypeSetup.ArmPath)
            .GetComponentsInChildren<SocketView>(true).Single(s => s.AllowedTypes.Contains(PartType.ArmCover));
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_GAME" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var link = prefab.GetComponent<PartViewConfigLink>();
            if (link == null || link.Config == null || link.Config.PartType != PartType.Arm || path == ArmCoverPrototypeSetup.ArmPath) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentsInChildren<SocketView>(true).Any(s => s.AllowedTypes.Contains(PartType.ArmCover))) continue;
                var copy = UnityEngine.Object.Instantiate(template.gameObject, root.transform, false);
                copy.name = template.name;
                copy.transform.SetLocalPositionAndRotation(template.transform.localPosition, template.transform.localRotation);
                copy.transform.localScale = template.transform.localScale;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    private static void ReplaceMovementHandle()
    {
        var ui = UnityEngine.Object.FindFirstObjectByType<ArmCoverAdjustmentView>(FindObjectsInactive.Include);
        var so = new SerializedObject(ui);
        var handles = so.FindProperty("handles");
        for (int i = 0; i < handles.arraySize; i++)
        {
            var handle = (ArmCoverScaleHandle)handles.GetArrayElementAtIndex(i).objectReferenceValue;
            if (handle.Axis != 3) continue;
            Undo.RecordObject(handle.GetComponent<MeshFilter>(), "Arrow movement handle");
            handle.GetComponent<MeshFilter>().sharedMesh = GetArrowMesh();
            var collider = handle.GetComponent<BoxCollider>();
            Undo.RecordObject(collider, "Arrow handle hit target"); collider.size = new Vector3(2.4f, 1.8f, 1.7f);
        }
    }

    public static Mesh GetArrowMesh()
    {
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(ArrowPath);
        if (saved != null) return saved;
        var outline = new[] { new Vector2(-1,-.16f), new Vector2(.15f,-.16f), new Vector2(.15f,-.45f),
            new Vector2(1,0), new Vector2(.15f,.45f), new Vector2(.15f,.16f), new Vector2(-1,.16f) };
        var vertices = new Vector3[14];
        for (int i = 0; i < 7; i++) { vertices[i] = new Vector3(outline[i].x, outline[i].y, .18f); vertices[i+7] = new Vector3(outline[i].x, outline[i].y, -.18f); }
        var front = new[] { 0,1,5, 0,5,6, 2,3,1, 1,3,5, 5,3,4 };
        var triangles = new List<int>(72); triangles.AddRange(front);
        for (int i = 0; i < front.Length; i += 3) { triangles.Add(front[i]+7); triangles.Add(front[i+2]+7); triangles.Add(front[i+1]+7); }
        for (int i = 0; i < 7; i++) { int j = (i+1)%7; triangles.AddRange(new[] { i,j+7,j, i,i+7,j+7 }); }
        var mesh = new Mesh { name = "MoveAlongArrow", vertices = vertices, triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, ArrowPath);
        return mesh;
    }
}
#endif
