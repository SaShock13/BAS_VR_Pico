using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using Zenject;
using Zenject.Internal;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling) throw new System.Exception("Editor not ready");
        for(int i=0;i<SceneManager.sceneCount;i++)
            if(SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/BootMenu.unity",OpenSceneMode.Single);
        var roots=scene.GetRootGameObjects();
        int missing=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var cameras=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(false)).ToArray();
        var buttons=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).ToArray();
        var menu=roots.SelectMany(r=>r.GetComponentsInChildren<StartMenu>(true)).Single();
        var serialized=new SerializedObject(menu);
        if(serialized.FindProperty("startBtn").objectReferenceValue==null || serialized.FindProperty("startBtnText").objectReferenceValue==null)
            throw new System.Exception("Menu view references missing");
        if(missing!=0 || cameras.Length!=1 || buttons.Length!=1)throw new System.Exception("Unexpected menu structure");
        if(buttons[0].onClick.GetPersistentTarget(0)!=menu || buttons[0].onClick.GetPersistentMethodName(0)!="StartConstructor")
            throw new System.Exception("Start button callback missing");
        var colliders=roots.SelectMany(r=>r.GetComponentsInChildren<DroneAssemblyAnimation>(true)).Single().GetComponentsInChildren<Collider>(true);
        if(colliders.Length!=0)throw new System.Exception("Animation still has physics colliders");
        foreach(var renderer in roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)))
            foreach(var material in renderer.sharedMaterials)
                if(material==null || material.shader==null || !material.shader.isSupported)throw new System.Exception("Invalid material on "+renderer.name);
        result.Log("Menu references valid; missing scripts=0; active cameras=1; button callback=StartConstructor; animation colliders=0");
        bool valid=ZenUnityEditorUtil.SaveThenRunPreserveSceneSetup(()=>ZenUnityEditorUtil.ValidateCurrentSceneSetup());
        if(!valid)throw new System.Exception("Zenject validation failed");
        result.Log("Zenject menu validation passed");
        MainMenuNavigationVerification.Run();
        result.Log("Navigation verification passed: duplicate requests, error/retry, disposal");
    }
}
