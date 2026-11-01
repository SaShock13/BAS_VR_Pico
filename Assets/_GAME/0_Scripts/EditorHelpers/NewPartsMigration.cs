#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class NewPartsMigration
{
    public const string Root = "Assets/_GAME/Prefabs/NewParts";
    public const string ConfigRoot = "Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts";
    private const string Parts = "Assets/_GAME/Prefabs/Parts/";
    private static int removedScripts;

    [MenuItem("Tools/BAS VR/Migrate New Parts")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Exit Play Mode and Prefab Mode before migration.");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.DefaultGroup == null)
            throw new InvalidOperationException("Addressables settings/default group is missing.");
        var referenceGuid = AssetDatabase.AssetPathToGUID(Parts + "Controller1.prefab");
        var referenceGroup = settings.FindAssetEntry(referenceGuid)?.parentGroup ?? settings.DefaultGroup;
        var group = settings.FindGroup("New Parts") ??
            settings.CreateGroup("New Parts", false, false, true, referenceGroup.Schemas);
        Directory.CreateDirectory(ConfigRoot);
        AssetDatabase.Refresh();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { Root });
        Array.Sort(guids, StringComparer.Ordinal);
        removedScripts = 0;
        var configs = new List<PartConfig>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PartType type = InferType(path);
            string relativePath = path.Substring(Root.Length + 1);
            string readableKey = relativePath.Substring(0, relativePath.Length - ".prefab".Length);
            string configPath = ConfigRoot + "/" + readableKey + "Config.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(configPath).Replace('\\', '/')))
                AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PartViewConfigLink>()?.Config
                ?? AssetDatabase.LoadAssetAtPath<PartConfig>(configPath);
            bool newConfig = config == null;
            if (newConfig)
            {
                if (type == PartType.Motor)
                    config = UnityEngine.Object.Instantiate(LoadTemplateConfig("Motor2306.asset"));
                else if (type == PartType.FlyController)
                {
                    config = ScriptableObject.CreateInstance<FlightControllerConfig>();
                    ((FlightControllerConfig)config).HasGyro = true;
                    ((FlightControllerConfig)config).HasAccelerometer = true;
                }
                else config = ScriptableObject.CreateInstance<PartConfig>();
                config.name = Path.GetFileNameWithoutExtension(path);
                config.PartId = "newparts/" + readableKey;
                config.PrefabAddress = "Parts/" + readableKey;
                config.PartType = type;
                AssetDatabase.CreateAsset(config, configPath);
            }
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Prepare(contents, config, newConfig);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            config.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(config);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.SetAddress(config.PrefabAddress);
            configs.Add(config);
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        int installers = RegisterInScenes(configs);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log($"NewParts: migrated {configs.Count} prefabs; removed {removedScripts} missing scripts; registered in {installers} installers.");
    }

    private static PartConfig LoadTemplateConfig(string file) =>
        AssetDatabase.LoadAssetAtPath<PartConfig>("Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/" + file);

    private static PartType InferType(string path)
    {
        if (path.Contains("/Bodies/")) return PartType.Body;
        if (path.Contains("/Rays/") || Path.GetFileName(path).StartsWith("D7Rays")) return PartType.Rays;
        if (path.Contains("/Legs/") || Path.GetFileName(path).StartsWith("Gears")) return PartType.Legs;
        if (path.Contains("/Propellers/") || Path.GetFileName(path).StartsWith("Propeller")) return PartType.Propeller;
        if (Path.GetFileName(path).StartsWith("Engine")) return PartType.Motor;
        if (Path.GetFileName(path) == "MainBoard.prefab") return PartType.FlyController;
        throw new InvalidOperationException("Unknown part category: " + path);
    }

    private static void Prepare(GameObject root, PartConfig config, bool newConfig)
    {
        // Unpack nested legacy instances in this asset only, preserving their visual data.
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != null && t != root.transform && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // Old frame prefabs contain complete demonstration motors under socket anchors.
        // They are separate runtime parts in the new architecture.
        foreach (var t in root.GetComponentsInChildren<Transform>(true).Reverse())
            if (t != null && t != root.transform &&
                (t.name.StartsWith("TestSocketEngine_Socketable", StringComparison.Ordinal) ||
                 (t.name.StartsWith("Engine (", StringComparison.Ordinal) && t.GetComponent<Rigidbody>() != null)))
                UnityEngine.Object.DestroyImmediate(t.gameObject);

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            removedScripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            int missingName = t.name.IndexOf(" (Missing Prefab", StringComparison.Ordinal);
            if (missingName >= 0) t.name = t.name.Substring(0, missingName);
        }
        foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
            UnityEngine.Object.DestroyImmediate(audio);
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            UnityEngine.Object.DestroyImmediate(animator);
        foreach (var grab in root.GetComponentsInChildren<XRGrabInteractable>(true))
            UnityEngine.Object.DestroyImmediate(grab);
        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
            if (body.gameObject != root) UnityEngine.Object.DestroyImmediate(body);
        foreach (var view in root.GetComponentsInChildren<DronePartView>(true))
            UnityEngine.Object.DestroyImmediate(view); // Added and initialized by Clean_AssemblySystem.

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.SetActive(true);
        var rigidbody = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
        if (newConfig)
        {
            if (config.PartType != PartType.Motor) config.Mass = Mathf.Max(0.001f, rigidbody.mass);
        }
        rigidbody.mass = config.Mass;
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.None;

        Bounds bounds = GetVisualBounds(root);
        if (newConfig) config.LocalCenterOfMass = bounds.center;
        var com = root.GetComponentInChildren<CenterOfMassMarker>(true);
        if (com == null) com = new GameObject("CenterOfMass").AddComponent<CenterOfMassMarker>();
        com.transform.SetParent(root.transform, false);
        com.transform.localPosition = config.LocalCenterOfMass;
        var link = root.GetComponent<PartViewConfigLink>() ?? root.AddComponent<PartViewConfigLink>();
        link.Config = config;

        ConfigureSockets(root, config.PartType, bounds);
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            if (collider.isTrigger && collider.GetComponent<SocketView>() == null)
                UnityEngine.Object.DestroyImmediate(collider);

        int partLayer = AssetDatabase.LoadAssetAtPath<GameObject>(Parts + "Controller1.prefab").layer;
        root.layer = partLayer;
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = partLayer;

        // Use compound primitive colliders instead of imported non-convex meshes.
        foreach (var mesh in root.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mesh.sharedMesh != null && mesh.GetComponent<BoxCollider>() == null)
            {
                var box = mesh.gameObject.AddComponent<BoxCollider>();
                box.center = mesh.sharedMesh.bounds.center;
                box.size = mesh.sharedMesh.bounds.size;
                box.isTrigger = mesh.isTrigger;
                box.enabled = mesh.enabled;
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        var physicsColliders = root.GetComponentsInChildren<Collider>(true)
            .Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.GetComponentInParent<SocketView>() == null).ToArray();
        if (physicsColliders.Length == 0)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = Vector3.Max(bounds.size, Vector3.one * 0.01f);
            physicsColliders = new Collider[] { collider };
        }
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(Parts + "Controller1.prefab").GetComponent<XRGrabInteractable>();
        var interactable = root.AddComponent<XRGrabInteractable>();
        EditorUtility.CopySerialized(template, interactable);
        interactable.attachTransform = null;
        interactable.secondaryAttachTransform = null;
        interactable.colliders.Clear();
        interactable.colliders.AddRange(physicsColliders);
        interactable.interactionLayers = 1;
        interactable.retainTransformParent = false;
        interactable.throwOnDetach = false;
        interactable.trackScale = false;

        if (config.PartType == DroneAssemblyRules.RootPartType)
        {
            var simulation = root.GetComponent<DronePhysicsSimulation>() ?? root.AddComponent<DronePhysicsSimulation>();
            var data = new SerializedObject(simulation);
            data.FindProperty("_rigidbody").objectReferenceValue = rigidbody;
            data.ApplyModifiedPropertiesWithoutUndo();
            simulation.enabled = false; // FlightController enables it after initialization.
        }
        RaysRootMigration.ConfigurePrefab(root, config.PartType);
    }

    private static Bounds GetVisualBounds(GameObject root)
    {
        bool found = false;
        Bounds result = new Bounds();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var bounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                else result.Encapsulate(point);
            }
        }
        if (!found) throw new InvalidOperationException("No visual mesh in " + root.name);
        return result;
    }

    private static PartType? AnchorType(string name, PartType owner)
    {
        if (name.StartsWith("NewParts_", StringComparison.Ordinal) && name.EndsWith("Socket", StringComparison.Ordinal) &&
            Enum.TryParse(name.Substring(9, name.Length - 15), out PartType generatedType)) return generatedType;
        if (name.StartsWith("EngineSocket") || name.StartsWith("MotorSocket")) return PartType.Motor;
        if (name.StartsWith("PropellerSocket")) return PartType.Propeller;
        if (name.StartsWith("SocketElectronicPlate") || name.StartsWith("ControllerSocket")) return PartType.FlyController;
        if (name.StartsWith("BatterySocket")) return PartType.Battery;
        if (name.StartsWith("LegsSocket") || name.StartsWith("GearSocket")) return PartType.Legs;
        if (name.StartsWith("ShellSocket") && owner == PartType.Rays) return PartType.Body;
        return null;
    }

    private static void ConfigureSockets(GameObject root, PartType owner, Bounds bounds)
    {
        var anchors = new List<(Transform transform, PartType type)>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var kind = AnchorType(t.name, owner);
            if (!kind.HasValue || t == root.transform) continue;
            bool nested = false;
            for (var p = t.parent; p != null && p != root.transform; p = p.parent)
                if (AnchorType(p.name, owner).HasValue) { nested = true; break; }
            if (!nested) anchors.Add((t, kind.Value));
        }
        if (owner == PartType.Body || owner == PartType.Rays)
        {
            AddFallback(anchors, root.transform, PartType.Battery, bounds.center + Vector3.up * bounds.extents.y);
            AddFallback(anchors, root.transform, PartType.Legs, bounds.center - Vector3.up * bounds.extents.y);
            AddFallback(anchors, root.transform, PartType.FlyController, bounds.center);
            if (owner == PartType.Rays) AddFallback(anchors, root.transform, PartType.Body, bounds.center);
        }
        if (owner == PartType.Motor)
            AddFallback(anchors, root.transform, PartType.Propeller, bounds.center + Vector3.up * bounds.extents.y);

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (var anchor in anchors)
        {
            var target = anchor.transform;
            var view = target.GetComponent<SocketView>() ?? target.gameObject.AddComponent<SocketView>();
            var data = new SerializedObject(view);
            var id = data.FindProperty("_socketId");
            if (string.IsNullOrWhiteSpace(id.stringValue) || !usedIds.Add(id.stringValue))
            {
                do { id.stringValue = "mount/" + anchor.type + "/" + index++; } while (!usedIds.Add(id.stringValue));
            }
            var allowed = data.FindProperty("AllowedTypes");
            allowed.arraySize = 1;
            allowed.GetArrayElementAtIndex(0).intValue = (int)anchor.type;
            var attach = target.Find("NewPartsAttachPoint");
            if (attach == null) { attach = new GameObject("NewPartsAttachPoint").transform; attach.SetParent(target, false); }
            data.FindProperty("AttachPoint").objectReferenceValue = attach;
            var interactor = target.GetComponent<HoverOnlySocketInteractor>() ?? target.gameObject.AddComponent<HoverOnlySocketInteractor>();
            interactor.interactionLayers = -1;
            interactor.attachTransform = attach;
            interactor.showInteractableHoverMeshes = false;
            foreach (var old in target.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(old);
            var trigger = target.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.1f / Mathf.Max(0.001f, Mathf.Abs(target.lossyScale.x), Mathf.Abs(target.lossyScale.y), Mathf.Abs(target.lossyScale.z));
            data.FindProperty("_socketInteractor").objectReferenceValue = interactor;
            data.FindProperty("_previewAnchor").objectReferenceValue = attach;
            data.ApplyModifiedPropertiesWithoutUndo();
            target.gameObject.SetActive(true);
        }
    }

    private static void AddFallback(List<(Transform transform, PartType type)> anchors, Transform root, PartType type, Vector3 position)
    {
        if (anchors.Any(a => a.type == type)) return;
        var t = new GameObject("NewParts_" + type + "Socket").transform;
        t.SetParent(root, false);
        t.localPosition = position;
        anchors.Add((t, type));
    }

    private static int RegisterInScenes(List<PartConfig> configs)
    {
        int count = 0;
        var active = SceneManager.GetActiveScene();
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool changed = false;
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var installer in root.GetComponentsInChildren<AssemblyInstaller>(true))
                    {
                        var data = new SerializedObject(installer);
                        var list = data.FindProperty("partConfigs");
                        var existing = new HashSet<UnityEngine.Object>();
                        for (int i = 0; i < list.arraySize; i++) existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
                        foreach (var config in configs)
                            if (existing.Add(config))
                            {
                                int i = list.arraySize++;
                                list.GetArrayElementAtIndex(i).objectReferenceValue = config;
                                changed = true;
                            }
                        data.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(installer);
                        count++;
                    }
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        return count;
    }

    [MenuItem("Tools/BAS VR/Validate New Parts")]
    public static void Validate()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var ids = new HashSet<string>();
        int parts = 0, sockets = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + path);
            var link = root.GetComponent<PartViewConfigLink>();
            if (link == null || link.Config == null || link.Config.Prefab != root || !ids.Add(link.Config.PartId))
                throw new InvalidOperationException("Invalid config: " + path);
            if (settings.FindAssetEntry(guid)?.address != link.Config.PrefabAddress)
                throw new InvalidOperationException("Addressables mismatch: " + path);
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 1 ||
                root.GetComponentsInChildren<XRGrabInteractable>(true).Length != 1 ||
                root.GetComponent<XRGrabInteractable>().colliders.Count == 0 ||
                root.GetComponentsInChildren<DronePartView>(true).Length != 0)
                throw new InvalidOperationException("Invalid runtime components: " + path);
            var socketIds = new HashSet<string>();
            foreach (var socket in root.GetComponentsInChildren<SocketView>(true))
            {
                var interactor = socket.GetComponent<HoverOnlySocketInteractor>();
                var trigger = socket.GetComponent<SphereCollider>();
                bool legacyRootMount = link.Config.PartType == PartType.Body &&
                    socket.SocketId.StartsWith("mount/Rays/", StringComparison.Ordinal) &&
                    socket.AllowedTypes != null && socket.AllowedTypes.Length == 0 &&
                    interactor != null && !interactor.enabled && trigger != null && !trigger.enabled;
                if (!socketIds.Add(socket.SocketId) || socket.AttachPoint == null ||
                    socket.AllowedTypes == null || (socket.AllowedTypes.Length == 0 && !legacyRootMount) ||
                    interactor == null || trigger?.isTrigger != true)
                    throw new InvalidOperationException("Invalid socket: " + path + "/" + socket.name);
                sockets++;
            }
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        throw new InvalidOperationException("Missing material/shader: " + path);
            parts++;
        }
        Debug.Log($"NewParts validation passed: {parts} registered assets, {sockets} unique per-part sockets.");
    }
}
#endif
