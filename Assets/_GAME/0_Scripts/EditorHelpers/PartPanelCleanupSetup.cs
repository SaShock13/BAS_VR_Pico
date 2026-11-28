#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PartPanelCleanupSetup
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        // Remove only references to the obsolete transform section. Cover controls,
        // fitting content and gizmos remain intact.
        foreach (var scenePath in new[] { "Assets/Scenes/ConstructorScene.unity", "Assets/Scenes/GarageScene.unity" })
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var cover in root.GetComponentsInChildren<ArmCoverAdjustmentView>(true))
            {
                var binding = new SerializedObject(cover);
                var sections = binding.FindProperty("standardSections");
                var keep = Enumerable.Range(0, sections.arraySize).Select(i => sections.GetArrayElementAtIndex(i).objectReferenceValue)
                    .Where(x => x != null && x.name != "AdjustmentPanel").ToArray();
                if (keep.Length == sections.arraySize) continue;
                sections.arraySize = keep.Length;
                for (int i = 0; i < keep.Length; i++) sections.GetArrayElementAtIndex(i).objectReferenceValue = keep[i];
                binding.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(cover);
                changed = true;
            }
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        const string panelPath = "Assets/_GAME/Prefabs/UI/Curved Part Screen.prefab";
        var panel = PrefabUtility.LoadPrefabContents(panelPath);
        try
        {
            foreach (var legacy in panel.GetComponentsInChildren<Transform>(true).Where(t => t.name == "AdjustmentPanel").ToArray())
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            PrefabUtility.SaveAsPrefabAsset(panel, panelPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(panel); }
        var shader = Shader.Find("BAS_VR/World UI Depth Background");
        if (shader == null) throw new InvalidOperationException("Card background shader missing");
        const string materialPath = "Assets/_GAME/Prefabs/UI/PartHoverInfoDepth.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        else { material.shader = shader; EditorUtility.SetDirty(material); }
        var card = PrefabUtility.LoadPrefabContents(PartHoverInfoSetup.PrefabPath);
        try
        {
            card.GetComponent<Image>().material = material;
            var binding = new SerializedObject(card.GetComponent<PartHoverInfoView>());
            binding.FindProperty("widthInMenuCells").floatValue = 2.025f;
            binding.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(card, PartHoverInfoSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(card); }
        AssetDatabase.SaveAssets();
    }
}
#endif
