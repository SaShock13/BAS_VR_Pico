#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class RaysRootMigration
{
    [MenuItem("Tools/BAS VR/Configure Rays Root and Detail Grab")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Exit Play Mode and Prefab Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:PartConfig"))
        {
            var config = AssetDatabase.LoadAssetAtPath<PartConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (config.Prefab == null || (config.PartType != PartType.Rays && config.PartType != PartType.Body)) continue;
            string path = AssetDatabase.GetAssetPath(config.Prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigurePrefab(root, config.PartType);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        const string scenePath = "Assets/Scenes/ConstructorScene.unity";
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.IsValid();
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        var installer = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AssemblyInstaller>(true)).Single();
        var input = installer.GetComponent<AssemblyDetailModeInput>() ?? installer.gameObject.AddComponent<AssemblyDetailModeInput>();
        typeof(AssemblyDetailModeInput).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(input, null);
        EditorUtility.SetDirty(input);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"Configured {count} body/rays prefabs and X/Space detail input in ConstructorScene.");
    }

    public static void ConfigurePrefab(GameObject root, PartType type)
    {
        if (type == PartType.Body)
        {
            foreach (var legacyView in root.GetComponents<DroneView>())
            {
                var legacy = new SerializedObject(legacyView);
                var colliderObject = legacy.FindProperty("mainTriggerGO").objectReferenceValue as GameObject;
                var legacyCollider = legacy.FindProperty("mainTrigger").objectReferenceValue as Collider;
                if (legacyCollider != null) legacyCollider.enabled = false;
                if (colliderObject != null)
                    foreach (var oldCollider in colliderObject.GetComponents<Collider>()) oldCollider.enabled = false;
                UnityEngine.Object.DestroyImmediate(legacyView);
            }
            // Preserve legacy mount IDs/transforms for old saves, but never attach a frame below a shell again.
            foreach (var socket in root.GetComponentsInChildren<SocketView>(true))
            {
                if (!socket.AllowedTypes.Contains(PartType.Rays)) continue;
                socket.AllowedTypes = socket.AllowedTypes.Where(t => t != PartType.Rays).ToArray();
                if (socket.AllowedTypes.Length != 0) continue;
                var interactor = socket.GetComponent<HoverOnlySocketInteractor>();
                if (interactor != null) interactor.enabled = false;
                foreach (var socketCollider in socket.GetComponents<Collider>()) socketCollider.enabled = false;
            }
            var simulation = root.GetComponent<DronePhysicsSimulation>();
            if (simulation != null) UnityEngine.Object.DestroyImmediate(simulation);
            return;
        }
        if (type != PartType.Rays) return;
        var grab = root.GetComponent<XRGrabInteractable>();
        var collider = grab.colliders.OfType<BoxCollider>().FirstOrDefault();
        if (collider == null) throw new InvalidOperationException("Rays needs a primitive grab collider: " + root.name);
        var envelope = root.GetComponent<DroneGrabEnvelope>() ?? root.AddComponent<DroneGrabEnvelope>();
        var envelopeSettings = new SerializedObject(envelope);
        envelopeSettings.FindProperty("mainCollider").objectReferenceValue = collider;
        envelopeSettings.ApplyModifiedPropertiesWithoutUndo();
        var simulationOnFrame = root.GetComponent<DronePhysicsSimulation>() ?? root.AddComponent<DronePhysicsSimulation>();
        var physicsSettings = new SerializedObject(simulationOnFrame);
        physicsSettings.FindProperty("_rigidbody").objectReferenceValue = root.GetComponent<Rigidbody>();
        physicsSettings.ApplyModifiedPropertiesWithoutUndo();
        simulationOnFrame.enabled = false;
        if (root.GetComponentsInChildren<SocketView>(true).Any(s => s.AllowedTypes.Contains(PartType.Body))) return;

        // Match the frame's visual center; designers can adjust this mount like every other socket.
        bool found = false;
        var bounds = new Bounds();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;
            var p = root.transform.InverseTransformPoint(renderer.bounds.center);
            if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; }
            else bounds.Encapsulate(p);
        }
        var mount = new GameObject("NewParts_BodySocket");
        mount.layer = root.layer;
        mount.transform.SetParent(root.transform, false);
        mount.transform.localPosition = bounds.center;
        var anchor = new GameObject("NewPartsAttachPoint").transform;
        anchor.SetParent(mount.transform, false);
        var socketView = mount.AddComponent<SocketView>();
        socketView.AllowedTypes = new[] { PartType.Body };
        socketView.AttachPoint = anchor;
        var interactorOnMount = mount.AddComponent<HoverOnlySocketInteractor>();
        interactorOnMount.interactionLayers = -1;
        interactorOnMount.attachTransform = anchor;
        interactorOnMount.showInteractableHoverMeshes = false;
        var trigger = mount.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.1f / Mathf.Max(0.001f, Mathf.Abs(mount.transform.lossyScale.x), Mathf.Abs(mount.transform.lossyScale.y), Mathf.Abs(mount.transform.lossyScale.z));
        var settings = new SerializedObject(socketView);
        settings.FindProperty("_socketId").stringValue = DroneAssemblyRules.BodySocketId;
        settings.FindProperty("_socketInteractor").objectReferenceValue = interactorOnMount;
        settings.FindProperty("_previewAnchor").objectReferenceValue = anchor;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
