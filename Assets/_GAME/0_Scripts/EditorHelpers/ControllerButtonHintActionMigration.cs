#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ControllerButtonHintActionMigration
{
    [MenuItem("Tools/BAS VR/Migrate Mission Controller Hints to Input Actions")]
    public static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Migrate definitions outside Play Mode.");
        var menu = Resources.FindObjectsOfTypeAll<LeftHandMenu>().FirstOrDefault(m => m.gameObject.scene.IsValid());
        var menuReference = menu != null
            ? new SerializedObject(menu).FindProperty("leftHandMenuAction").objectReferenceValue as InputActionReference : null;
        var references = AssetDatabase.LoadAllAssetsAtPath(ControllerButtonHintBindingsBuilder.InputPath)
            .OfType<InputActionReference>().ToArray();
        int migrated = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:MissionStepData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var step = AssetDatabase.LoadAssetAtPath<MissionStepData>(path);
            if (step == null) continue;
            var data = new SerializedObject(step);
            var legacy = data.FindProperty("legacyControllerButtonHint");
            if (legacy == null || legacy.intValue == 0) continue;
            var id = (ControllerButtonHintId)legacy.intValue;
            var current = data.FindProperty("ControllerButtonAction");
            if (current.objectReferenceValue == null)
            {
                var reference = id == ControllerButtonHintId.PartsMenu && menuReference != null
                    ? menuReference : references.FirstOrDefault(r => r.action != null && r.action.name == id.ToString());
                if (reference == null)
                    throw new InvalidOperationException("No action reference for legacy hint " + id + " in " + path);
                Undo.RecordObject(step, "Migrate controller hint to InputAction");
                current.objectReferenceValue = reference;
            }
            legacy.intValue = 0;
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(step);
            migrated++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Migrated " + migrated + " mission controller hints to InputActionReference.");
    }
}
#endif
