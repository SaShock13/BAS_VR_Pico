#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoverHandleRenderingSetup
{
    [MenuItem("Tools/BAS VR/Setup Transparent Cover Handles")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var shader = Shader.Find("BAS_VR/Cover Handle Overlay");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Handle shader missing or unsupported.");
        for (int i = 0; i < 5; i++)
        {
            string path = i < 4 ? "Assets/_GAME/Prefabs/NewParts/ArmCover/Handle_" + i + ".mat"
                : MotorCoverCatalogSetup.Prefabs + "UniformScaleHandle.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var color = material.GetColor("_BaseColor");
            color.a = .55f;
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            material.renderQueue = 3050;
            EditorUtility.SetDirty(material);
        }
        // Sorting order is evaluated before transparent queue. Menus therefore render after
        // handles, even though their regular UI shaders use queue 3000 and ZTest LEqual.
        const string scenePath = "Assets/Scenes/ConstructorScene.unity";
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var root in scene.GetRootGameObjects()) ConfigureMenus(root, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        const string menuPath = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
        var contents = PrefabUtility.LoadPrefabContents(menuPath);
        try { ConfigureMenus(contents, false); PrefabUtility.SaveAsPrefabAsset(contents, menuPath); }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets();
        Debug.Log("Five translucent handle materials configured; menu surfaces retain depth-based occlusion.");
    }

    private static void ConfigureMenus(GameObject root, bool scene)
    {
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;
            if (scene) Undo.RecordObject(canvas, "Menu sorting above cover handles");
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(1, canvas.sortingOrder);
            if (scene) PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
        }
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool screen = false;
            foreach (var material in renderer.sharedMaterials)
                if (material != null && (material.shader.name == "BAS_VR/Alpha Screen" ||
                    material.shader.name == "BAS_VR/Sci-Fi Screen Cutout")) screen = true;
            if (!screen) continue;
            if (scene) Undo.RecordObject(renderer, "Screen sorting above cover handles");
            renderer.sortingOrder = Mathf.Max(1, renderer.sortingOrder);
            if (scene) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
}
#endif
