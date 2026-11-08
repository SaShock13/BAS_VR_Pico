#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class ControllerButtonHintBindingsBuilder
{
    public readonly struct Binding
    {
        public readonly ControllerButtonHintId Id;
        public readonly bool Left;
        public readonly string Bone;
        public readonly string Control;
        public readonly Vector3 Rotation;
        public Binding(ControllerButtonHintId id, bool left, string bone, string control, Vector3 rotation = default)
        { Id = id; Left = left; Bone = bone; Control = control; Rotation = rotation; }
    }

    public static readonly Binding[] Bindings =
    {
        new Binding(ControllerButtonHintId.PartsMenu, true, "SkinLMenu", "MenuButton"),
        new Binding(ControllerButtonHintId.LeftPrimary, true, "SkinLX", "PrimaryButton"),
        new Binding(ControllerButtonHintId.LeftSecondary, true, "SkinLY", "SecondaryButton"),
        new Binding(ControllerButtonHintId.LeftTrigger, true, "SkinLTrigger", "Trigger", new Vector3(-15f, 0f, 0f)),
        new Binding(ControllerButtonHintId.LeftGrip, true, "SkinLGrip", "Grip", new Vector3(0f, 12f, 0f)),
        // PICO's right-hand bones are named X/Y; the physical labels are A/B.
        new Binding(ControllerButtonHintId.RightPrimary, false, "SkinRX", "PrimaryButton"),
        new Binding(ControllerButtonHintId.RightSecondary, false, "SkinRY", "SecondaryButton"),
        new Binding(ControllerButtonHintId.RightTrigger, false, "SkinRTrigger", "Trigger", new Vector3(-15f, 0f, 0f)),
        new Binding(ControllerButtonHintId.RightGrip, false, "SkinRGrip", "Grip", new Vector3(0f, 12f, 0f)),
        new Binding(ControllerButtonHintId.RightThumbstick, false, "SkinRJoystick", "Primary2DAxisClick"),
        new Binding(ControllerButtonHintId.LeftThumbstick, true, "SkinLJoystick", "Primary2DAxisClick")
    };
    public const string Folder = "Assets/_GAME/Art/ControllerHints";
    public const string InputPath = Folder + "/ControllerHintButtons.inputactions";

    [MenuItem("Tools/BAS VR/Build Controller Button Hint Bindings")]
    public static void BuildCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build controller bindings outside Play Mode.");
        var menu = Resources.FindObjectsOfTypeAll<LeftHandMenu>()
            .Single(m => m.gameObject.scene.IsValid() && m.gameObject.scene.isLoaded);
        var scene = menu.gameObject.scene;
        var loaders = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .Where(m => m != null && m.gameObject.scene == scene && m.GetType().Name == "PXR_ControllerLoader")
            .ToArray();
        var left = loaders.Single(m => m.name == "[Building Block] Left Controller").transform;
        var right = loaders.Single(m => m.name == "[Building Block] Right Controller").transform;
        var menuAction = new SerializedObject(menu).FindProperty("leftHandMenuAction").objectReferenceValue;
        var trackingReferences = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(menuAction))
            .OfType<InputActionReference>().ToArray();
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/_GAME/Art", "ControllerHints");
        BuildInputAsset();
        var buttonReferences = AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>().ToArray();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ControllerButtonHint.mat");
        if (material == null)
            throw new InvalidOperationException("Shared button hint material is missing.");
        foreach (var binding in Bindings)
        {
            var loader = binding.Left ? left : right;
            string side = binding.Left ? "L" : "R";
            var loaderComponent = loaders.Single(m => m.transform == loader);
            var prefab = new SerializedObject(loaderComponent).FindProperty("PICO_4U_" + side)
                .objectReferenceValue as GameObject;
            if (prefab == null)
                throw new InvalidOperationException("PICO 4 Ultra model is not assigned on " + loader.name);
            string meshPath = binding.Id == ControllerButtonHintId.PartsMenu
                ? Folder + "/ControllerMenuButtonSurface.asset"
                : Folder + "/ControllerButton_" + binding.Id + "_Surface.asset";
            var mesh = ControllerButtonHintMeshBuilder.Build(prefab, binding.Bone, meshPath);
            string name = "ControllerButtonHint_" + binding.Id;
            var target = loader.parent.Find(name);
            if (target == null)
            {
                var created = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                Undo.RegisterCreatedObjectUndo(created, "Create controller button hint");
                target = created.transform;
                target.SetParent(loader.parent, false);
            }
            var filter = target.GetComponent<MeshFilter>();
            Undo.RecordObject(filter, "Set button surface");
            filter.sharedMesh = mesh;
            var renderer = target.GetComponent<MeshRenderer>();
            Undo.RecordObject(renderer, "Configure button surface rendering");
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.enabled = false;
            var press = target.GetComponent<ControllerButtonPressVisual>();
            if (press == null) press = Undo.AddComponent<ControllerButtonPressVisual>(target.gameObject);
            Undo.RecordObject(press, "Configure button motion");
            var pressData = new SerializedObject(press);
            pressData.FindProperty("rotatePress").boolValue = binding.Rotation != Vector3.zero;
            pressData.FindProperty("pressEulerAngles").vector3Value = binding.Rotation;
            pressData.ApplyModifiedProperties();
            press.enabled = false;
            var view = loader.GetComponents<ControllerButtonHintView>().FirstOrDefault(v =>
                new SerializedObject(v).FindProperty("button").intValue == (int)binding.Id);
            if (view == null) view = Undo.AddComponent<ControllerButtonHintView>(loader.gameObject);
            Undo.RecordObject(view, "Configure controller button hint binding");
            var data = new SerializedObject(view);
            data.FindProperty("button").intValue = (int)binding.Id;
            data.FindProperty("visual").objectReferenceValue = press;
            data.FindProperty("buttonBoneName").stringValue = binding.Bone;
            data.FindProperty("buttonAction").objectReferenceValue = buttonReferences.First(r =>
                r.action != null && r.action.name == binding.Id.ToString());
            data.FindProperty("manageButtonAction").boolValue = true;
            string mapName = binding.Left ? "XRI Left" : "XRI Right";
            data.FindProperty("isTrackedAction").objectReferenceValue = trackingReferences.First(r =>
                r.action != null && r.action.actionMap.name == mapName && r.action.name == "Is Tracked");
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(press);
            Debug.Log("Controller hint: " + binding.Id + " -> " + binding.Bone + " (" + mesh.triangles.Length / 3 + " triangles)");
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidateCurrentScene();
    }

    [MenuItem("Tools/BAS VR/Validate Controller Button Hint Bindings")]
    public static void ValidateCurrentScene()
    {
        if (Bindings.Length != Enum.GetValues(typeof(ControllerButtonHintId)).Length - 1
            || Bindings.Select(b => b.Id).Distinct().Count() != Bindings.Length)
            throw new InvalidOperationException("Every non-None controller hint must have exactly one binding.");
        var menu = Resources.FindObjectsOfTypeAll<LeftHandMenu>().Single(m => m.gameObject.scene.IsValid());
        var views = Resources.FindObjectsOfTypeAll<ControllerButtonHintView>()
            .Where(v => v.gameObject.scene == menu.gameObject.scene).ToArray();
        foreach (var binding in Bindings)
        {
            var matching = views.Where(v => new SerializedObject(v).FindProperty("button").intValue == (int)binding.Id).ToArray();
            if (matching.Length != 1)
                throw new InvalidOperationException("Expected exactly one view for " + binding.Id);
            var view = matching[0];
            var data = new SerializedObject(view);
            var visual = data.FindProperty("visual").objectReferenceValue as ControllerButtonPressVisual;
            var action = data.FindProperty("buttonAction").objectReferenceValue as InputActionReference;
            var tracking = data.FindProperty("isTrackedAction").objectReferenceValue as InputActionReference;
            string expectedLoader = binding.Left ? "[Building Block] Left Controller" : "[Building Block] Right Controller";
            string expectedTracking = binding.Left ? "XRI Left" : "XRI Right";
            string hand = binding.Left ? "LeftHand" : "RightHand";
            string expectedPath = "<XRController>{" + hand + "}/{" + binding.Control + "}";
            if (view.name != expectedLoader || visual == null || visual.transform.IsChildOf(view.transform)
                || data.FindProperty("buttonBoneName").stringValue != binding.Bone
                || !data.FindProperty("manageButtonAction").boolValue
                || action == null || action.action == null || action.action.name != binding.Id.ToString()
                || AssetDatabase.GetAssetPath(action) != InputPath
                || action.action.bindings.Count != 1 || action.action.bindings[0].effectivePath != expectedPath
                || tracking == null || tracking.action == null || tracking.action.actionMap.name != expectedTracking)
                throw new InvalidOperationException("Incomplete controller hint binding: " + binding.Id);
            var mesh = visual.GetComponent<MeshFilter>().sharedMesh;
            var motion = new SerializedObject(visual);
            if (mesh == null || mesh.vertexCount == 0 || mesh.triangles.Length == 0
                || motion.FindProperty("rotatePress").boolValue != (binding.Rotation != Vector3.zero)
                || motion.FindProperty("pressEulerAngles").vector3Value != binding.Rotation)
                throw new InvalidOperationException("Invalid controller button mesh/motion: " + binding.Id);
            if (!EditorApplication.isPlaying && (visual.enabled || visual.GetComponent<MeshRenderer>().enabled || action.action.enabled))
                throw new InvalidOperationException("Controller hint should be idle outside Play Mode: " + binding.Id);
        }
        Debug.Log("PASS: all " + Bindings.Length + " controller button hint bindings are configured.");
    }

    private static void BuildInputAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        bool temporary = asset == null;
        if (temporary)
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ControllerHintButtons";
        }
        var map = asset.FindActionMap("ControllerHints", false) ?? asset.AddActionMap("ControllerHints");
        foreach (var binding in Bindings)
        {
            var action = map.FindAction(binding.Id.ToString(), false)
                ?? map.AddAction(binding.Id.ToString(), InputActionType.Value);
            action.expectedControlType = "Axis";
            string hand = binding.Left ? "LeftHand" : "RightHand";
            string path = "<XRController>{" + hand + "}/{" + binding.Control + "}";
            if (action.bindings.Count == 0) action.AddBinding(path);
            else action.ChangeBinding(0).WithPath(path);
        }
        var json = asset.ToJson();
        if (!File.Exists(InputPath) || File.ReadAllText(InputPath) != json)
        {
            File.WriteAllText(InputPath, json);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
        }
        if (temporary) UnityEngine.Object.DestroyImmediate(asset);
    }
}
#endif
