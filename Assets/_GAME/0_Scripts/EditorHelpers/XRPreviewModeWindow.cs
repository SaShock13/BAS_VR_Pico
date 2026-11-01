#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRPreviewModeWindow : EditorWindow
{
    private const string LoaderType = "Unity.XR.PICO.LivePreview.PXR_PTLoader";
    private const string SimulatorPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    private const string SimulatorProperty = "m_AutomaticallyInstantiateSimulatorPrefab";
    private string error;

    [MenuItem("Tools/BAS VR/XR Preview Mode")]
    public static void Open()
    {
        var window = GetWindow<XRPreviewModeWindow>("XR Preview");
        window.minSize = new Vector2(340, 150);
        window.Show();
    }

    private static XRGeneralSettings Settings =>
        XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

    private static bool HasLivePreview(XRManagerSettings manager) =>
        manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == LoaderType);

    public static string CurrentMode
    {
        get
        {
            var simulator = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SimulatorPath);
            var settings = Settings;
            if (!simulator || !settings || !settings.Manager)
                return "Settings unavailable";
            var enabled = new SerializedObject(simulator).FindProperty(SimulatorProperty).boolValue;
            var live = HasLivePreview(settings.Manager);
            if (live && !enabled && settings.InitManagerOnStart)
                return "PICO Live Preview";
            if (!live && enabled)
                return new SerializedObject(simulator).FindProperty("m_SimulatorPrefab").objectReferenceValue
                    ? "XR Interaction Simulator" : "Simulator prefab missing";
            return "Mixed / manual settings";
        }
    }

    private void OnInspectorUpdate() => Repaint();

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current mode", CurrentMode, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Applies to the next Play session. XR Interaction Toolkit works in both modes.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            var toLive = CurrentMode != "PICO Live Preview";
            if (GUILayout.Button(toLive ? "Switch to PICO Live Preview" : "Switch to XR Interaction Simulator", GUILayout.Height(32)))
            {
                try { SetLivePreview(toLive); error = null; }
                catch (Exception exception) { error = exception.Message; Debug.LogException(exception); }
            }
        }
        if (!string.IsNullOrEmpty(error))
            EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    public static void SetLivePreview(bool enabled)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before switching.");
        var simulator = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SimulatorPath);
        var settings = Settings;
        if (!simulator || !settings || !settings.Manager)
            throw new InvalidOperationException("XR simulator or Standalone XR settings are missing.");
        var serialized = new SerializedObject(simulator);
        var property = serialized.FindProperty(SimulatorProperty);
        if (property == null)
            throw new InvalidOperationException("XR simulator settings format has changed.");

        var prefabProperty = serialized.FindProperty("m_SimulatorPrefab");
        if (!enabled && !prefabProperty.objectReferenceValue)
        {
            var classic = serialized.FindProperty("m_UseClassic").boolValue;
            var componentType = classic
                ? typeof(UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator)
                : typeof(UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator);
            var prefabName = classic ? "XR Device Simulator" : "XR Interaction Simulator";
            var prefab = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault(candidate => candidate && candidate.GetComponent(componentType));
            if (!prefab)
                throw new InvalidOperationException("Import the " + prefabName + " sample from XR Interaction Toolkit first.");
            prefabProperty.objectReferenceValue = prefab;
        }

        var manager = settings.Manager;
        // Only the desktop loader changes: Android/PICO build settings stay intact.
        if (HasLivePreview(manager) != enabled)
        {
            var succeeded = enabled
                ? XRPackageMetadataStore.AssignLoader(manager, LoaderType, BuildTargetGroup.Standalone)
                : XRPackageMetadataStore.RemoveLoader(manager, LoaderType, BuildTargetGroup.Standalone);
            if (!succeeded || HasLivePreview(manager) != enabled)
                throw new InvalidOperationException("Could not switch the PICO Live Preview loader.");
        }
        Undo.RecordObject(settings, "Switch XR preview mode");
        if (enabled)
            settings.InitManagerOnStart = true;
        property.boolValue = !enabled;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
#endif
