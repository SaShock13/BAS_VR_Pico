#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class AssemblyProjectMenuBuilder
{
    private static TMP_FontAsset _font;
    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(.5f,.5f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        return r;
    }
    private static TMP_Text Label(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize=25)
    {
        var r = Rect(name,parent,pos,size);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = _font; t.text=text; t.fontSize=fontSize; t.alignment=TextAlignmentOptions.Center;
        t.raycastTarget=false; t.richText=false;
        return t;
    }
    private static Button Button(string name, Transform parent, string text, Vector2 pos, Vector2 size)
    {
        var r=Rect(name,parent,pos,size);
        var image=r.gameObject.AddComponent<Image>(); image.color=new Color(.08f,.16f,.20f,.96f);
        var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image;
        Label("Label",r,text,Vector2.zero,size-Vector2.one*8);
        return b;
    }
    private static TMP_InputField Input(Transform parent, Vector2 pos)
    {
        var r=Rect("ProjectName",parent,pos,new Vector2(680,58));
        r.gameObject.AddComponent<Image>().color=new Color(.08f,.12f,.16f,1);
        var field=r.gameObject.AddComponent<TMP_InputField>();
        var viewport=Rect("Viewport",r,Vector2.zero,new Vector2(650,54));
        viewport.gameObject.AddComponent<RectMask2D>();
        var text=Label("Text",viewport,"",Vector2.zero,new Vector2(640,54));
        var placeholder=Label("Placeholder",viewport,"Имя сборки (можно оставить пустым)",Vector2.zero,new Vector2(640,54),22);
        placeholder.color=Color.gray;
        field.textViewport=viewport; field.textComponent=(TextMeshProUGUI)text; field.placeholder=placeholder;
        field.characterLimit=80; field.lineType=TMP_InputField.LineType.SingleLine;
        return field;
    }
    [MenuItem("Tools/BAS VR/Build project save menus")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var original=SceneManager.GetActiveScene();
        var boot=SceneManager.GetSceneByPath("Assets/Scenes/BootMenu.unity");
        bool close=!boot.isLoaded;
        if(close) boot=EditorSceneManager.OpenScene("Assets/Scenes/BootMenu.unity",OpenSceneMode.Additive);
        try
        {
            StartMenu menu=null;
            foreach(var root in boot.GetRootGameObjects()) { menu=root.GetComponentInChildren<StartMenu>(true); if(menu!=null) break; }
            if(menu==null) throw new InvalidOperationException("Boot StartMenu missing");
            var so=new SerializedObject(menu);
            var old=(Button)so.FindProperty("startBtn").objectReferenceValue;
            _font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
            if(_font==null) throw new InvalidOperationException("AssemblyProjectFont is missing");
            var canvas=menu.GetComponent<Canvas>();
            var existing=menu.transform.Find("AssemblyProjects");
            if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            // Work in existing world-space canvas units.
            var panel=Rect("AssemblyProjects",menu.transform,Vector2.zero,new Vector2(800,850));
            var canvasRect=(RectTransform)menu.transform;
            float scale=Mathf.Min(canvasRect.rect.width/800f,canvasRect.rect.height/850f)*.94f;
            panel.localScale=Vector3.one*scale;
            panel.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.06f,.98f);
            Label("Title",panel,"СБОРКИ ДРОНОВ",new Vector2(0,370),new Vector2(740,60),32);
            var input=Input(panel,new Vector2(0,290));
            var create=Button("Create",panel,"НОВАЯ СБОРКА",new Vector2(0,215),new Vector2(680,64));
            UnityEventTools.AddPersistentListener(create.onClick,menu.StartConstructor);
            var show=Button("ShowSaves",panel,"СОХРАНЁННЫЕ СБОРКИ",new Vector2(0,135),new Vector2(680,60));
            var rows=new Button[4];
            for(int i=0;i<rows.Length;i++) rows[i]=Button("SavedRow"+i,panel,"Сборка",new Vector2(0,50-i*76),new Vector2(720,64));
            var prev=Button("Previous",panel,"НАЗАД",new Vector2(-185,-270),new Vector2(320,56));
            var next=Button("Next",panel,"ДАЛЕЕ",new Vector2(185,-270),new Vector2(320,56));
            var status=Label("Status",panel,"",new Vector2(0,-350),new Vector2(730,75),22);
            so.FindProperty("startBtn").objectReferenceValue=create;
            so.FindProperty("startBtnText").objectReferenceValue=create.GetComponentInChildren<TMP_Text>();
            so.FindProperty("projectName").objectReferenceValue=input;
            so.FindProperty("showSaves").objectReferenceValue=show;
            so.FindProperty("previous").objectReferenceValue=prev;
            so.FindProperty("next").objectReferenceValue=next;
            so.FindProperty("statusText").objectReferenceValue=status;
            var a=so.FindProperty("savedRows"); a.arraySize=rows.Length;
            for(int i=0;i<rows.Length;i++) a.GetArrayElementAtIndex(i).objectReferenceValue=rows[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            if(old != null) old.gameObject.SetActive(false);
            var main=menu.transform.Find("MainPanel"); if(main!=null) main.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(boot); EditorSceneManager.SaveScene(boot);
        }
        finally { if(close) EditorSceneManager.CloseScene(boot,true); SceneManager.SetActiveScene(original); }
        var constructor=SceneManager.GetSceneByPath("Assets/Scenes/ConstructorScene.unity");
        if(!constructor.isLoaded) throw new InvalidOperationException("Open ConstructorScene for constructor menu setup");
        Saver_TEST1 saver=null;
        foreach(var root in constructor.GetRootGameObjects()) { saver=root.GetComponentInChildren<Saver_TEST1>(true); if(saver!=null) break; }
        if(saver==null) { var go=new GameObject("Assembly Save Actions"); SceneManager.MoveGameObjectToScene(go,constructor); saver=go.AddComponent<Saver_TEST1>(); }
        foreach(var root in constructor.GetRootGameObjects()) foreach(var menu in root.GetComponentsInChildren<AssemblyMenuView>(true))
        {
            var canvas=menu.GetComponentInChildren<Canvas>(true); if(canvas==null) continue;
            var existing=canvas.transform.Find("ProjectSaveControls"); if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var canvasRect=(RectTransform)canvas.transform;
            canvasRect.sizeDelta=new Vector2(canvasRect.sizeDelta.x,1250);
            var controls=Rect("ProjectSaveControls",canvas.transform,new Vector2(0,-435),new Vector2(1100,340));
            var name=Input(controls,new Vector2(-140,115));
            var rename=Button("Rename",controls,"ИМЯ",new Vector2(390,115),new Vector2(200,58));
            UnityEventTools.AddPersistentListener(rename.onClick,saver.RenameActive);
            var saverData=new SerializedObject(saver);
            saverData.FindProperty("projectName").objectReferenceValue=name;
            saverData.ApplyModifiedPropertiesWithoutUndo();
            var save=Button("Save",controls,"СОХРАНИТЬ",new Vector2(-225,40),new Vector2(420,60));
            var exit=Button("SaveExit",controls,"СОХРАНИТЬ И ВЫЙТИ",new Vector2(225,40),new Vector2(420,60));
            var discard=Button("Discard",controls,"ВЫЙТИ БЕЗ СОХРАНЕНИЯ…",new Vector2(0,-35),new Vector2(600,60));
            var confirm=Rect("ConfirmDiscard",controls,new Vector2(0,-110),new Vector2(850,65));
            var yes=Button("Yes",confirm,"ДА, ВЫЙТИ",new Vector2(-210,0),new Vector2(390,60));
            var cancel=Button("Cancel",confirm,"ОТМЕНА",new Vector2(210,0),new Vector2(390,60));
            UnityEventTools.AddPersistentListener(save.onClick,saver.SaveActive);
            UnityEventTools.AddPersistentListener(exit.onClick,saver.SaveAndExit);
            UnityEventTools.AddBoolPersistentListener(discard.onClick,confirm.gameObject.SetActive,true);
            UnityEventTools.AddPersistentListener(yes.onClick,saver.ExitWithoutSaving);
            UnityEventTools.AddBoolPersistentListener(cancel.onClick,confirm.gameObject.SetActive,false);
            confirm.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(constructor);
        // ConstructorScene may contain user edits; leave it dirty for the user to save.
        AssetDatabase.SaveAssets();
        Debug.Log("Project menus built. Boot saved; ConstructorScene changes remain unsaved with existing edits.");
    }
}
#endif
