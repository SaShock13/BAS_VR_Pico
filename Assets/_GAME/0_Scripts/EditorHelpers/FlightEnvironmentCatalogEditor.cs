#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[CustomEditor(typeof(FlightEnvironmentCatalog))]
public sealed class FlightEnvironmentCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Добавьте локацию, задайте уникальный ID и перетащите сцену окружения. Сцены автоматически включаются в Build Profiles. В окружении нужен FlightEnvironmentContext с двумя точками старта.", MessageType.Info);
        SceneField(serializedObject.FindProperty("coreScenePath"), "Общая сцена полёта");
        var entries = serializedObject.FindProperty("environments");
        int remove = -1;
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("title"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("description"));
            SceneField(entry.FindPropertyRelative("scenePath"), "Сцена окружения");
            if (GUILayoutButton("Удалить локацию")) remove = i;
            EditorGUILayout.EndVertical();
        }
        if (remove >= 0) entries.DeleteArrayElementAtIndex(remove);
        if (GUILayoutButton("Добавить локацию"))
        {
            int index = entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(index);
            foreach (var name in new[] { "id", "title", "description", "scenePath" })
                entry.FindPropertyRelative(name).stringValue = "";
        }
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("missions"),
            new UnityEngine.GUIContent("Полётные миссии"), true);
        if (serializedObject.ApplyModifiedProperties()) FlightEnvironmentBuildScenes.Sync((FlightEnvironmentCatalog)target);
    }

    private static bool GUILayoutButton(string text) => UnityEngine.GUILayout.Button(text);
    private static void SceneField(SerializedProperty path, string label)
    {
        var current = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
        EditorGUI.BeginChangeCheck();
        var scene = (SceneAsset)EditorGUILayout.ObjectField(label, current, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck()) path.stringValue = scene == null ? "" : AssetDatabase.GetAssetPath(scene);
    }
}

public sealed class FlightEnvironmentBuildScenes : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:FlightEnvironmentCatalog"))
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FlightEnvironmentCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            Validate(catalog);
            Sync(catalog);
        }
    }

    public static void Validate(FlightEnvironmentCatalog catalog)
    {
        if (catalog.Environments.Count == 0) throw new BuildFailedException("Flight environment catalog is empty.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in catalog.Environments)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id))
                throw new BuildFailedException("Flight environments need unique, nonempty IDs.");
            if (string.IsNullOrWhiteSpace(entry.title) || entry.scenePath == catalog.coreScenePath || !paths.Add(entry.scenePath))
                throw new BuildFailedException("Flight environments need titles and distinct environment scenes.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.scenePath) == null)
                throw new BuildFailedException("Missing environment scene: " + entry.scenePath);
        }
        var missionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in catalog.Missions)
        {
            var mission = entry?.mission;
            if (mission == null || string.IsNullOrWhiteSpace(mission.MissionId) || !missionIds.Add(mission.MissionId))
                throw new BuildFailedException("Flight missions need definitions with unique, nonempty IDs.");
            if (!ids.Contains(entry.environmentId))
                throw new BuildFailedException("Missing environment for mission: " + mission.MissionName);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(catalog.coreScenePath) == null)
            throw new BuildFailedException("Missing common flight scene.");
    }

    public static void Sync(FlightEnvironmentCatalog catalog)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (var path in new[] { catalog.coreScenePath }.Concat(catalog.Environments.Where(e => e != null).Select(e => e.scenePath)))
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = scenes.Find(s => s.path == path);
            if (scene == null) { scenes.Add(new EditorBuildSettingsScene(path, true)); changed = true; }
            else if (!scene.enabled) { scene.enabled = true; changed = true; }
        }
        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
