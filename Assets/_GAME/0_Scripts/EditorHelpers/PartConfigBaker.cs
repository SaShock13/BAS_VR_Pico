#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class PartConfigBaker
{
    [MenuItem("Tools/Bake Selected Part COM")]
    public static void BakeCenterOfMass()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("No prefab selected");
            return;
        }

        //DronePartView partView =
        //    selected.GetComponent<DronePartView>();

        //if (partView == null)
        //{
        //    Debug.LogError(
        //        "Selected object has no DronePartView");
        //    return;
        //}

        PartViewConfigLink link =
            selected.GetComponent<PartViewConfigLink>();

        if (link == null)
        {
            Debug.LogError(
                "No PartViewConfigLink found");
            return;
        }

        PartConfig config = link.Config;

        if (config == null)
        {
            Debug.LogError("Config is null");
            return;
        }

        CenterOfMassMarker marker =
            selected.GetComponentInChildren<CenterOfMassMarker>();

        if (marker == null)
        {
            Debug.LogError(
                "CenterOfMassMarker not found");
            return;
        }

        Undo.RecordObject(config, "Bake Center Of Mass");

        config.LocalCenterOfMass =
            marker.transform.localPosition;

        EditorUtility.SetDirty(config);

        Debug.Log(
            $"COM baked: {config.LocalCenterOfMass}");
    }
}

#endif