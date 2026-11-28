#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class FlightControllerMountSetup
{
    private const string Prefabs = "Assets/_GAME/Prefabs/NewParts/Tech/";
    private const string Configs = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/Tech/";

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var sourcePath = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "MainBoard.prefab") != null
            ? Prefabs + "MainBoard.prefab" : Prefabs + "FlyghtController.prefab";
        var source = PrefabUtility.LoadPrefabContents(sourcePath);
        Mesh smallMesh, largeMesh;
        try
        {
            smallMesh = MakeMesh(source, "FlyghtController", .020f, .024f, .008f);
            largeMesh = MakeMesh(source, "FlyghtController_30_5", .0305f, .0345f, .010f);
        }
        finally { PrefabUtility.UnloadPrefabContents(source); }
        MoveIfPresent(sourcePath, Prefabs + "FlyghtController.prefab");
        MoveIfPresent(Configs + "MainBoardConfig.asset", Configs + "FlyghtControllerConfig.asset");
        var small = AssetDatabase.LoadAssetAtPath<PartConfig>(Configs + "FlyghtControllerConfig.asset");
        if (small == null) throw new InvalidOperationException("Controller config missing");
        var previousId = small.PartId;
        small.LegacyPartIds = small.LegacyPartIds.Concat(new[] { previousId }).Where(x => !string.IsNullOrEmpty(x))
            .Distinct().Where(x => x != "newparts/Tech/FlyghtController").ToArray();
        small.PartId = "newparts/Tech/FlyghtController";
        small.PrefabAddress = "Parts/Tech/FlyghtController";
        var large = AssetDatabase.LoadAssetAtPath<PartConfig>(Configs + "FlyghtController_30_5Config.asset");
        if (large == null)
        {
            large = UnityEngine.Object.Instantiate(small);
            large.PartId = "newparts/Tech/FlyghtController_30_5";
            large.PrefabAddress = "Parts/Tech/FlyghtController_30_5";
            large.LegacyPartIds = Array.Empty<string>();
            AssetDatabase.CreateAsset(large, Configs + "FlyghtController_30_5Config.asset");
        }
        PreparePrefab("FlyghtController", smallMesh, small);
        PreparePrefab("FlyghtController_30_5", largeMesh, large);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(Prefabs + "FlyghtController.prefab"))?.parentGroup
            ?? settings.FindGroup("New Parts") ?? settings.DefaultGroup;
        foreach (var config in new[] { small, large })
        {
            config.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + config.PrefabAddress.Split('/').Last() + ".prefab");
            config.LocalCenterOfMass = config.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.center;
            EditorUtility.SetDirty(config);
            settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config.Prefab)), group).SetAddress(config.PrefabAddress);
        }
        var templatePlate = PrefabUtility.LoadPrefabContents("Assets/_GAME/Prefabs/NewParts/BottomPlate/R1_FrameBottomPlate.prefab");
        GameObject socketTemplate;
        try
        {
            socketTemplate = UnityEngine.Object.Instantiate(templatePlate.GetComponentsInChildren<SocketView>(true)
                .First(x => x.AllowedTypes.Contains(PartType.FlyController)).gameObject);
            socketTemplate.hideFlags = HideFlags.HideAndDontSave;
        }
        finally { PrefabUtility.UnloadPrefabContents(templatePlate); }
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_GAME/Prefabs/NewParts/BottomPlate" }))
                ConfigurePlate(AssetDatabase.GUIDToAssetPath(guid), socketTemplate);
        }
        finally { UnityEngine.Object.DestroyImmediate(socketTemplate); }
        ConfigureMenu(small, large);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        NewPartsMigration.RegisterInScenes(new List<PartConfig> { small, large });
        Debug.Log("Flight controller mounts configured: 20x20 and 30.5x30.5 mm.");
    }

    private static void MoveIfPresent(string from, string to)
    {
        if (from == to || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(from) == null) return;
        var error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }

    private static Mesh MakeMesh(GameObject source, string name, float pitch, float outer, float height)
    {
        string path = Prefabs + name + "_Mesh.asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null) return saved;
        var filter = source.GetComponentInChildren<MeshFilter>();
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
        mesh.name = name + "_Mesh";
        var matrix = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        float minY = vertices.Min(v => v.y), maxY = vertices.Max(v => v.y);
        // Four mounting-hole centres measured in the original model's root space.
        var backLeft = new Vector2(-.039827f, -.049396f);
        var backRight = new Vector2(.040398f, -.048880f);
        var frontLeft = new Vector2(-.040153f, .042536f);
        var frontRight = new Vector2(.040147f, .043085f);
        var center = (backLeft + backRight + frontLeft + frontRight) * .25f;
        var across = (backRight + frontRight - backLeft - frontLeft) * .5f;
        var forward = (frontLeft + frontRight - backLeft - backRight) * .5f;
        float determinant = across.x * forward.y - across.y * forward.x;
        var coordinates = vertices.Select(v =>
        {
            var delta = new Vector2(v.x, v.z) - center;
            return new Vector2((delta.x * forward.y - delta.y * forward.x) / determinant,
                (across.x * delta.y - across.y * delta.x) / determinant);
        }).ToArray();
        float minX = coordinates.Min(v => v.x), maxX = coordinates.Max(v => v.x);
        float minZ = coordinates.Min(v => v.y), maxZ = coordinates.Max(v => v.y);
        float Map(float value, float min, float max)
        {
            if (value < -.5f) return Mathf.Lerp(-outer * .5f, -pitch * .5f, Mathf.InverseLerp(min, -.5f, value));
            if (value > .5f) return Mathf.Lerp(pitch * .5f, outer * .5f, Mathf.InverseLerp(.5f, max, value));
            return value * pitch;
        }
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(Map(coordinates[i].x, minX, maxX),
                Mathf.InverseLerp(minY, maxY, vertices[i].y) * height, Map(coordinates[i].y, minZ, maxZ));
        mesh.vertices = vertices;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void PreparePrefab(string name, Mesh mesh, PartConfig config)
    {
        var path = Prefabs + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null
            ? path : Prefabs + "FlyghtController.prefab");
        try
        {
            root.name = name;
            var scaler = root.transform.Find("Scaler");
            scaler.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); scaler.localScale = Vector3.one;
            var model = root.GetComponentInChildren<MeshFilter>();
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); model.transform.localScale = Vector3.one;
            model.sharedMesh = mesh;
            model.GetComponent<MeshRenderer>().sharedMaterial = BoardMaterial(name);
            var collider = model.GetComponent<BoxCollider>();
            collider.center = mesh.bounds.center; collider.size = mesh.bounds.size;
            root.GetComponent<PartViewConfigLink>().Config = config;
            root.transform.Find("CenterOfMass").localPosition = mesh.bounds.center;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigurePlate(string path, GameObject template)
    {
        var plate = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var socket = plate.GetComponentsInChildren<SocketView>(true).FirstOrDefault(x => x.AllowedTypes.Contains(PartType.FlyController));
            if (socket == null)
            {
                var go = UnityEngine.Object.Instantiate(template, plate.transform, false);
                go.hideFlags = HideFlags.None; go.name = "FlightControllerSocket";
                socket = go.GetComponent<SocketView>();
            }
            var bounds = plate.GetComponentsInChildren<Renderer>().First().bounds;
            var localTop = plate.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y;
            socket.transform.SetParent(plate.transform, false);
            socket.transform.SetLocalPositionAndRotation(new Vector3(0, localTop + .001f, 0), Quaternion.identity);
            socket.transform.localScale = Vector3.one;
            socket.gameObject.SetActive(true);
            if (socket.AttachPoint != socket.transform)
            {
                socket.AttachPoint.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                socket.AttachPoint.localScale = Vector3.one;
            }
            var sphere = socket.GetComponent<SphereCollider>();
            if (sphere != null) { sphere.center = Vector3.zero; sphere.radius = .022f; }
            PrefabUtility.SaveAsPrefabAsset(plate, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(plate); }
    }

    public static void RepairMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        foreach (var name in new[] { "FlyghtController", "FlyghtController_30_5" })
        {
            var path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponentInChildren<MeshRenderer>().sharedMaterial = BoardMaterial(name);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        ConfigureMenu(AssetDatabase.LoadAssetAtPath<PartConfig>(Configs + "FlyghtControllerConfig.asset"),
            AssetDatabase.LoadAssetAtPath<PartConfig>(Configs + "FlyghtController_30_5Config.asset"));
        AssetDatabase.SaveAssets();
    }

    private static Material BoardMaterial(string name)
    {
        var path = Prefabs + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/_GAME/3D_Models/DronesParts/Electronic/MAinBoard.mat", path))
                throw new InvalidOperationException("Cannot create board material: " + name);
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        var tint = name == "FlyghtController" ? new Color(.30f, .85f, .55f, 1) : new Color(.35f, .55f, 1f, 1);
        Undo.RecordObject(material, "Distinguish flight controller variants");
        material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureMenu(PartConfig small, PartConfig large)
    {
        const string path = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
        var menu = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var publisher = menu.GetComponent<MenuPartCreationView>();
            var data = new SerializedObject(publisher);
            var entries = data.FindProperty("entries");
            Transform original = null;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("config").objectReferenceValue == small)
                    original = ((Button)entries.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue).transform;
            if (original == null) throw new InvalidOperationException("Controller menu entry missing");
            foreach (var config in new[] { small, large })
            {
                int index = -1;
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("config").objectReferenceValue == config) index = i;
                var tile = index >= 0 ? ((Button)entries.GetArrayElementAtIndex(index).FindPropertyRelative("button").objectReferenceValue).gameObject
                    : UnityEngine.Object.Instantiate(original.gameObject, original.parent, false);
                tile.name = config.Prefab.name;
                tile.GetComponent<PartViewConfigLink>().Config = config;
                // Discard the previously normalized prefab hierarchy. Normalize a
                // single mesh once, directly beneath the cell's rotation pivot.
                foreach (Transform child in tile.transform.Cast<Transform>().ToArray())
                    if (child.name == "PreviewRotation") UnityEngine.Object.DestroyImmediate(child.gameObject);
                var rotation = new GameObject("PreviewRotation").transform;
                rotation.SetParent(tile.transform, false);
                rotation.localRotation = Quaternion.Euler(60, 0, 0);
                var visual = new GameObject("Board preview", typeof(MeshFilter), typeof(MeshRenderer));
                visual.transform.SetParent(rotation, false);
                var model = visual.GetComponent<MeshFilter>();
                var mesh = config.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
                model.sharedMesh = mesh;
                var renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = config.Prefab.GetComponentInChildren<MeshRenderer>().sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                model.transform.localRotation = Quaternion.identity;
                float previewSize = config == small ? 140f : 175f;
                float factor = previewSize / mesh.bounds.size.magnitude;
                model.transform.localScale = Vector3.one * factor;
                model.transform.localPosition = -mesh.bounds.center * factor;
                var button = tile.GetComponent<Button>();
                var tooltip = tile.GetComponent<MenuTooltip>() ?? tile.AddComponent<MenuTooltip>();
                tooltip.Configure(config == small ? "Контроллер полёта: 20×20 мм" : "Контроллер полёта: 30,5×30,5 мм");
                if (index < 0)
                {
                    index = entries.arraySize++;
                    var entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("button").objectReferenceValue = button;
                    entry.FindPropertyRelative("config").objectReferenceValue = config;
                    while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
                    UnityEventTools.AddIntPersistentListener(button.onClick, publisher.RequestCreatePart, index);
                }
                entries.GetArrayElementAtIndex(index).FindPropertyRelative("previewTransform").objectReferenceValue = model.transform;
                var rows = menu.GetComponentsInChildren<ObjectRowManager>(true);
                var electronics = rows.First(r => r.name == "OtherParts");
                foreach (var row in rows) row.allObjects.RemoveAll(item => item == tile);
                electronics.allObjects.Add(tile);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var row in menu.GetComponentsInChildren<ObjectRowManager>(true)) row.RefreshLayout();
            PrefabUtility.SaveAsPrefabAsset(menu, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(menu); }
    }
}
#endif
