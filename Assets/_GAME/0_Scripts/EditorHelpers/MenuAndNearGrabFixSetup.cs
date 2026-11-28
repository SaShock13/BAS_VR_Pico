#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class MenuAndNearGrabFixSetup
{
    public static void Verify()
    {
        var rig = PrefabUtility.LoadPrefabContents("Assets/[Building Block] XR Origin (XR Rig) XRI300.prefab");
        try
        {
            var views = rig.GetComponentsInChildren<NearGrabControllerVisual>(true);
            if (views.Length != 2) throw new Exception("Both controller hands must be configured");
            foreach (var view in views)
            {
                var bindings = new SerializedObject(view);
                var roots = bindings.FindProperty("visualRoots");
                var visuals = Enumerable.Range(0, roots.arraySize).Select(i => (GameObject)roots.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var states = visuals.Select(v => v.activeSelf).ToArray();
                view.SetRegion(NearFarInteractor.Region.Near);
                if (visuals.Any(v => v.activeSelf) || !view.gameObject.activeInHierarchy) throw new Exception("Near grab did not hide only visuals");
                view.SetRegion(NearFarInteractor.Region.Near);
                view.SetRegion(NearFarInteractor.Region.None);
                for (int i = 0; i < visuals.Length; i++)
                    if (visuals[i].activeSelf != states[i]) throw new Exception("Controller state not restored on release");
                view.SetRegion(NearFarInteractor.Region.Far);
                for (int i = 0; i < visuals.Length; i++)
                    if (visuals[i].activeSelf != states[i]) throw new Exception("Far grab hides controller");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(rig); }
        var card = PrefabUtility.LoadPrefabContents(PartHoverInfoSetup.PrefabPath);
        try
        {
            foreach (Transform child in card.transform)
                if (child.localPosition.z >= -.5f) throw new Exception("Card foreground still coplanar with background");
        }
        finally { PrefabUtility.UnloadPrefabContents(card); }
        var menu = PrefabUtility.LoadPrefabContents("Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab");
        try
        {
            foreach (var link in menu.GetComponentsInChildren<PartViewConfigLink>(true))
            {
                if (link.Config == null || link.Config.PartType != PartType.Esc || link.GetComponent<Button>() == null) continue;
                var model = link.GetComponentInChildren<MeshFilter>(true);
                float diagonal = link.transform.InverseTransformVector(model.transform.TransformVector(model.sharedMesh.bounds.size)).magnitude;
                float expected = link.Config.Prefab.name == "ESC_Single" ? 110 : link.Config.Prefab.name == "ESC_20x20" ? 140 : 175;
                if (Mathf.Abs(diagonal - expected) > .1f) throw new Exception("ESC preview hierarchy size incorrect");
                Debug.Log("ESC preview verified: " + link.name + " diagonal=" + diagonal);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(menu); }
        Debug.Log("PASS near/far visual restoration, non-coplanar card foreground, ESC preview sizes.");
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var card = PrefabUtility.LoadPrefabContents(PartHoverInfoSetup.PrefabPath);
        try
        {
            foreach (Transform child in card.transform)
            {
                var position = child.localPosition; position.z = -1; child.localPosition = position;
            }
            PrefabUtility.SaveAsPrefabAsset(card, PartHoverInfoSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(card); }
        const string menuPath = "Assets/_GAME/Prefabs/UI/AssemblyMenu.prefab";
        var menu = PrefabUtility.LoadPrefabContents(menuPath);
        try
        {
            var entries = new SerializedObject(menu.GetComponent<MenuPartCreationView>());
            var list = entries.FindProperty("entries");
            foreach (var link in menu.GetComponentsInChildren<PartViewConfigLink>(true))
            {
                if (link.Config == null || link.Config.PartType != PartType.Esc || link.GetComponent<Button>() == null) continue;
                foreach (var child in link.transform.Cast<Transform>().Where(t => t.name == "PreviewRotation").ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                var rotation = new GameObject("PreviewRotation").transform; rotation.SetParent(link.transform, false);
                rotation.localRotation = Quaternion.Euler(60, 0, 0);
                var model = new GameObject("ESC preview", typeof(MeshFilter), typeof(MeshRenderer));
                model.transform.SetParent(rotation, false);
                var source = link.Config.Prefab.GetComponentInChildren<MeshFilter>();
                var mesh = source.sharedMesh;
                model.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = model.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                float size = link.Config.Prefab.name == "ESC_Single" ? 110 : link.Config.Prefab.name == "ESC_20x20" ? 140 : 175;
                float factor = size / mesh.bounds.size.magnitude;
                model.transform.localScale = Vector3.one * factor; model.transform.localPosition = -mesh.bounds.center * factor;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue == link.GetComponent<Button>())
                        list.GetArrayElementAtIndex(i).FindPropertyRelative("previewTransform").objectReferenceValue = model.transform;
            }
            entries.ApplyModifiedPropertiesWithoutUndo();
            foreach (var row in menu.GetComponentsInChildren<ObjectRowManager>(true)) row.RefreshLayout();
            PrefabUtility.SaveAsPrefabAsset(menu, menuPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(menu); }
        foreach (var path in new[] { "Assets/[Building Block] XR Origin (XR Rig) XRI300.prefab", "Assets/_GAME/Prefabs/Env/[Building Block] XR Origin (XR Rig) XRI300.prefab" })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var rig = PrefabUtility.LoadPrefabContents(path);
            try { ConfigureRig(rig); PrefabUtility.SaveAsPrefabAsset(rig, path); }
            finally { PrefabUtility.UnloadPrefabContents(rig); }
        }
        AssetDatabase.SaveAssets();
    }

    public static int ConfigureRig(GameObject rig)
    {
        int count = 0;
        foreach (var interactor in rig.GetComponentsInChildren<NearFarInteractor>(true))
        {
            var controller = interactor.transform.parent;
            var visuals = controller.Cast<Transform>().Where(t => t.name.Contains("Controller Visual") ||
                t.name == "[Building Block] Left Controller" || t.name == "[Building Block] Right Controller").Select(t => t.gameObject).ToArray();
            if (visuals.Length == 0) continue;
            var view = interactor.GetComponent<NearGrabControllerVisual>() ?? interactor.gameObject.AddComponent<NearGrabControllerVisual>();
            var binding = new SerializedObject(view);
            binding.FindProperty("interactor").objectReferenceValue = interactor;
            var array = binding.FindProperty("visualRoots"); array.arraySize = visuals.Length;
            for (int i = 0; i < visuals.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = visuals[i];
            binding.ApplyModifiedProperties();
            count++;
        }
        return count;
    }
}
#endif
