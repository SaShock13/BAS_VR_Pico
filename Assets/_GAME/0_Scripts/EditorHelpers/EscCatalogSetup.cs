#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class EscCatalogSetup
{
    public const string Prefabs = "Assets/_GAME/Prefabs/NewParts/Esc/";
    public const string Configs = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/Esc/";
    private const string Source = "Assets/_GAME/3D_Models/DronesParts/Electronic/ESC_20x20.fbx";

    [MenuItem("Tools/BAS VR/Setup Single ESC 10 mm")]
    public static void RunSingle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        const string name = "ESC_Single";
        string prefabPath = Prefabs + name + ".prefab";
        string configPath = Configs + name + "Config.asset";
        var config = AssetDatabase.LoadAssetAtPath<PartConfig>(configPath);
        if (config == null || config.Prefab == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/3D_Models/DronesParts/Electronic/ESC_Single.fbx")
                .GetComponentInChildren<MeshFilter>(true);
            var mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            mesh.name = name + "_Mesh";
            var imported = Matrix4x4.TRS(Vector3.zero, source.transform.localRotation, source.transform.localScale);
            var vertices = mesh.vertices.Select(imported.MultiplyPoint3x4).ToArray();
            // The long dimension is X. Width is the short, transverse Z dimension of the PCB.
            float width = vertices.Max(v => v.z) - vertices.Min(v => v.z);
            float factor = .010f / width;
            float bottom = vertices.Min(v => v.y);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = new Vector3(vertices[i].x, vertices[i].y - bottom, vertices[i].z) * factor;
            mesh.vertices = vertices;
            var normals = mesh.normals;
            var normalMatrix = imported.inverse.transpose;
            for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, Prefabs + mesh.name + ".asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PartConfig>();
                config.name = name + "Config";
                config.PartId = "newparts/Esc/" + name;
                config.PrefabAddress = "Parts/Esc/" + name;
                config.PartType = PartType.Esc;
                config.HasConstantMaterial = true;
                config.Mass = .005f; // Initial physics estimate in kg.
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.DisplayName = "Одиночный регулятор оборотов — 10 мм";
            config.Description = "Регулятор оборотов для одного мотора. Требуется по одному регулятору на каждый мотор.";
            config.InfoCharacteristics = new[]
            {
                new PartConfig.InfoCharacteristic { Label = "Ширина", Value = "10", Unit = "мм" },
                new PartConfig.InfoCharacteristic { Label = "Каналы", Value = "1", Unit = "" }
            };
            config.LocalCenterOfMass = mesh.bounds.center;
            var root = PrefabUtility.LoadPrefabContents(Prefabs + "ESC_20x20.prefab");
            try
            {
                root.name = name;
                foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Mount_")).ToArray())
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                var filter = root.GetComponentInChildren<MeshFilter>(true);
                filter.sharedMesh = mesh;
                filter.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                var collider = root.GetComponent<BoxCollider>();
                collider.center = mesh.bounds.center;
                collider.size = mesh.bounds.size;
                root.GetComponent<Rigidbody>().mass = config.Mass;
                root.GetComponent<PartViewConfigLink>().Config = config;
                root.GetComponentInChildren<CenterOfMassMarker>(true).transform.localPosition = config.LocalCenterOfMass;
                config.Prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), settings.FindGroup("New Parts") ?? settings.DefaultGroup)
                .SetAddress(config.PrefabAddress);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(config);
        }
        ArmCoverPrototypeSetup.AddMenuEntry(config, "OtherParts");
        AssetDatabase.SaveAssets();
        NewPartsMigration.RegisterInScenes(new List<PartConfig> { config });
        var bounds = config.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
        Require(Mathf.Abs(bounds.size.z - .010f) < .000001f && config.Prefab.transform.localScale == Vector3.one,
            "Single ESC width/root scale invalid.");
        NewPartsMigration.Validate();
        Debug.Log("Single ESC created: width 10 mm, uniformly resized geometry, one regulator per motor; Electronics menu registered.");
    }

    [MenuItem("Tools/BAS VR/Setup ESC 4-in-1 Catalog")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Configs);
        AssetDatabase.Refresh();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        var filter = source.GetComponentInChildren<MeshFilter>(true);
        var configs = new List<PartConfig>
        {
            Create(filter, "ESC_20x20", .020f, "20×20"),
            Create(filter, "ESC_30_5x30_5", .0305f, "30,5×30,5")
        };
        foreach (var config in configs) ArmCoverPrototypeSetup.AddMenuEntry(config, "OtherParts");
        AssetDatabase.SaveAssets();
        NewPartsMigration.RegisterInScenes(configs);
        Verify();
        Debug.Log("Two ESC 4-in-1 prefabs/configs registered in Electronics: 20x20 and 30.5x30.5 mm.");
    }

    private static PartConfig Create(MeshFilter source, string name, float pitch, string size)
    {
        string prefabPath = Prefabs + name + ".prefab";
        string configPath = Configs + name + "Config.asset";
        var config = AssetDatabase.LoadAssetAtPath<PartConfig>(configPath);
        if (config != null && config.Prefab != null) return config;
        var mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
        mesh.name = name + "_Mesh";
        var imported = Matrix4x4.TRS(Vector3.zero, source.transform.localRotation, source.transform.localScale);
        var matrix = Matrix4x4.Scale(new Vector3(pitch / .020f, 1, pitch / .020f)) * imported;
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        float bottom = vertices.Min(v => v.y);
        for (int i = 0; i < vertices.Length; i++) vertices[i].y -= bottom;
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
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PartConfig>();
            config.name = name + "Config";
            config.PartType = PartType.Esc;
            config.PartId = "newparts/Esc/" + name;
            config.PrefabAddress = "Parts/Esc/" + name;
            config.Mass = .010f; // Initial editor estimate in kg; no unverified ratings in the part card.
            config.HasConstantMaterial = true;
            AssetDatabase.CreateAsset(config, configPath);
        }
        config.DisplayName = "Регулятор 4 в 1 — " + size + " мм";
        config.Description = "Регулятор оборотов 4 в 1 для управления четырьмя моторами. Монтажные отверстия " + size + " мм.";
        config.InfoCharacteristics = new[]
        {
            new PartConfig.InfoCharacteristic { Label = "Монтаж", Value = size, Unit = "мм" },
            new PartConfig.InfoCharacteristic { Label = "Каналы", Value = "4", Unit = "" }
        };
        config.LocalCenterOfMass = saved.bounds.center;
        var root = new GameObject(name);
        try
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/Prefabs/Parts/Controller1.prefab");
            root.layer = template.layer;
            var model = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(root.transform, false);
            model.layer = root.layer;
            model.GetComponent<MeshFilter>().sharedMesh = saved;
            model.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            var box = root.AddComponent<BoxCollider>();
            box.center = saved.bounds.center;
            box.size = Vector3.Max(saved.bounds.size, Vector3.one * .002f);
            var body = root.AddComponent<Rigidbody>();
            body.mass = config.Mass;
            body.isKinematic = true;
            body.useGravity = false;
            root.AddComponent<PartViewConfigLink>().Config = config;
            var grab = root.AddComponent<XRGrabInteractable>();
            EditorUtility.CopySerialized(template.GetComponent<XRGrabInteractable>(), grab);
            grab.attachTransform = null;
            grab.secondaryAttachTransform = null;
            grab.colliders.Clear();
            grab.colliders.Add(box);
            grab.interactionLayers = 1;
            grab.retainTransformParent = false;
            grab.throwOnDetach = false;
            grab.trackScale = false;
            var com = new GameObject("CenterOfMass").AddComponent<CenterOfMassMarker>();
            com.transform.SetParent(root.transform, false);
            com.transform.localPosition = config.LocalCenterOfMass;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var marker = new GameObject("Mount_" + x + "_" + z).transform;
                marker.SetParent(root.transform, false);
                marker.localPosition = new Vector3(x * pitch * .5f, 0, z * pitch * .5f);
            }
            config.Prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("New Parts") ?? settings.DefaultGroup;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), group).SetAddress(config.PrefabAddress);
        EditorUtility.SetDirty(config);
        EditorUtility.SetDirty(settings);
        return config;
    }

    [MenuItem("Tools/BAS VR/Verify ESC Catalog")]
    public static void Verify()
    {
        var small = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ESC_20x20.prefab");
        var large = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ESC_30_5x30_5.prefab");
        var a = small.GetComponentInChildren<MeshFilter>().sharedMesh;
        var b = large.GetComponentInChildren<MeshFilter>().sharedMesh;
        Require(a != b && a.vertexCount == b.vertexCount, "Independent mesh variants missing.");
        var av = a.vertices; var bv = b.vertices;
        for (int i = 0; i < av.Length; i++)
            Require(Vector3.Distance(new Vector3(av[i].x * 1.525f, av[i].y, av[i].z * 1.525f), bv[i]) < .000001f,
                "Large ESC geometry/unchanged thickness mismatch.");
        foreach (var root in new[] { small, large })
        {
            var config = root.GetComponent<PartViewConfigLink>().Config;
            Require(root.transform.localScale == Vector3.one && config.PartType == PartType.Esc &&
                config.Description.Contains("4 в 1"), "ESC identity/description/root scale invalid.");
            float pitch = root == small ? .020f : .0305f;
            Require(Mathf.Abs(root.transform.Find("Mount_1_1").localPosition.x - root.transform.Find("Mount_-1_1").localPosition.x - pitch) < .000001f,
                "Mounting pitch invalid.");
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab");
            var row = menu.GetComponentsInChildren<ObjectRowManager>(true).Single(r => r.name == "OtherParts");
            Require(row.allObjects.Count(o => o.GetComponent<PartViewConfigLink>()?.Config == config) == 1,
                "ESC Electronics entry missing/duplicated.");
        }
        NewPartsMigration.Validate();
        Debug.Log("PASS: ESC mesh variants, unchanged thickness, unit scale, mount pitch, descriptions and Electronics menu.");
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
#endif
