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
    public static void BuildBootMenu(StartMenu menu)
    {
        _font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
        if(_font==null) throw new InvalidOperationException("AssemblyProjectFont is missing");
        var existing=menu.transform.Find("AssemblyProjects");
        if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var legacy=menu.transform.Find("MainPanel");
        if(legacy!=null) legacy.gameObject.SetActive(false);
        var panel=Rect("AssemblyProjects",menu.transform,Vector2.zero,new Vector2(800,850));
        var canvasRect=(RectTransform)menu.transform;
        panel.localScale=Vector3.one*Mathf.Min(canvasRect.rect.width/800f,canvasRect.rect.height/850f)*.94f;
        panel.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.06f,1);
        Label("Title",panel,"Конструктор дронов 2.0",new Vector2(0,365),new Vector2(750,70),36);
        var main=Rect("MainWindow",panel,new Vector2(0,-15),new Vector2(760,620));
        var create=Button("NewAssembly",main,"Новая сборка",new Vector2(0,220),new Vector2(680,72));
        var saves=Button("LoadAssemblies",main,"Загрузить сборки",new Vector2(0,120),new Vector2(680,72));
        var settings=Button("Settings",main,"Настройки",new Vector2(0,20),new Vector2(680,72));
        var about=Button("About",main,"О разработчиках",new Vector2(0,-80),new Vector2(680,72));
        var quit=Button("Quit",main,"Выход",new Vector2(0,-180),new Vector2(680,72));
        var fresh=Rect("NewWindow",panel,Vector2.zero,new Vector2(760,620));
        Label("Heading",fresh,"Новая сборка",new Vector2(0,265),new Vector2(720,60),30);
        var input=Input(fresh,new Vector2(0,190));
        ((TMP_Text)input.placeholder).text="Введите имя сборки";
        input.shouldHideSoftKeyboard=true;
        var keys=new System.Collections.Generic.List<Button>();
        string[] letters={"1234567890", "ЙЦУКЕНГШЩЗХ", "ФЫВАПРОЛДЖЭ", "ЯЧСМИТЬБЮЁЪ"};
        for(int row=0;row<letters.Length;row++)
        for(int col=0;col<letters[row].Length;col++)
        {
            string letter=letters[row][col].ToString();
            var key=Button("Key_"+letter,fresh,letter,new Vector2((col-(letters[row].Length-1)*.5f)*62,105-row*66),new Vector2(56,58));
            UnityEventTools.AddStringPersistentListener(key.onClick,menu.TypeCharacter,letter);
            keys.Add(key);
        }
        var space=Button("Space",fresh,"Пробел",new Vector2(-115,-165),new Vector2(420,55));
        UnityEventTools.AddStringPersistentListener(space.onClick,menu.TypeCharacter," "); keys.Add(space);
        var erase=Button("Backspace",fresh,"Стереть",new Vector2(230,-165),new Vector2(210,55));
        UnityEventTools.AddPersistentListener(erase.onClick,menu.Backspace); keys.Add(erase);
        var ok=Button("CreateOK",fresh,"OK",new Vector2(-180,-260),new Vector2(320,64));
        var cancelNew=Button("Cancel",fresh,"Отмена",new Vector2(180,-260),new Vector2(320,64));
        var load=Rect("LoadWindow",panel,Vector2.zero,new Vector2(760,620));
        Label("Heading",load,"Загрузить сборки",new Vector2(0,265),new Vector2(720,60),30);
        var rows=new Button[4];
        for(int i=0;i<rows.Length;i++) rows[i]=Button("SavedRow"+i,load,"Сборка",new Vector2(0,175-i*83),new Vector2(720,72));
        var prev=Button("Previous",load,"Назад",new Vector2(-185,-175),new Vector2(320,56));
        var next=Button("Next",load,"Далее",new Vector2(185,-175),new Vector2(320,56));
        var closeLoad=Button("Close",load,"Отмена",new Vector2(0,-260),new Vector2(680,64));
        var prefs=Rect("SettingsWindow",panel,Vector2.zero,new Vector2(760,620));
        Label("Heading",prefs,"Настройки",new Vector2(0,265),new Vector2(720,60),30);
        Label("Info",prefs,"Настройки появятся в следующем обновлении.",new Vector2(0,70),new Vector2(660,180),28);
        var savePrefs=Button("Save",prefs,"OK — сохранить",new Vector2(-180,-260),new Vector2(320,64));
        var cancelPrefs=Button("Cancel",prefs,"Отмена",new Vector2(180,-260),new Vector2(320,64));
        var credits=Rect("AboutWindow",panel,Vector2.zero,new Vector2(760,620));
        Label("Heading",credits,"О разработчиках",new Vector2(0,265),new Vector2(720,60),30);
        Label("Info",credits,"Конструктор дронов 2.0\n\nVR-приложение для сборки и испытания дронов.\n\nИнформация о команде появится здесь позже.",new Vector2(0,40),new Vector2(660,330),28);
        var closeAbout=Button("Close",credits,"OK",new Vector2(0,-260),new Vector2(680,64));
        var status=Label("Status",panel,"",new Vector2(0,-365),new Vector2(730,75),22);
        UnityEventTools.AddPersistentListener(create.onClick,menu.OpenNew);
        UnityEventTools.AddPersistentListener(saves.onClick,menu.OpenSaves);
        UnityEventTools.AddPersistentListener(settings.onClick,menu.OpenSettings);
        UnityEventTools.AddPersistentListener(about.onClick,menu.OpenAbout);
        UnityEventTools.AddPersistentListener(quit.onClick,menu.Quit);
        UnityEventTools.AddPersistentListener(ok.onClick,menu.StartConstructor);
        UnityEventTools.AddPersistentListener(savePrefs.onClick,menu.AcceptSettings);
        foreach(var close in new[]{cancelNew,closeLoad,cancelPrefs,closeAbout}) UnityEventTools.AddPersistentListener(close.onClick,menu.CloseWindow);
        var so=new SerializedObject(menu);
        string[] names={"mainWindow","newWindow","loadWindow","settingsWindow","aboutWindow","startBtn","startBtnText","projectName","showSaves","previous","next","statusText"};
        UnityEngine.Object[] refs={main.gameObject,fresh.gameObject,load.gameObject,prefs.gameObject,credits.gameObject,ok,ok.GetComponentInChildren<TMP_Text>(),input,saves,prev,next,status};
        for(int i=0;i<names.Length;i++) so.FindProperty(names[i]).objectReferenceValue=refs[i];
        SetArray(so,"savedRows",rows);
        SetArray(so,"navigationButtons",new[]{create,saves,settings,about,quit,cancelNew,closeLoad,savePrefs,cancelPrefs,closeAbout});
        SetArray(so,"keyboardKeys",keys.ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var window in new[]{fresh,load,prefs,credits}) window.gameObject.SetActive(false);
    }
    private static void SetArray(SerializedObject so,string name,Button[] buttons)
    {
        var array=so.FindProperty(name); array.arraySize=buttons.Length;
        for(int i=0;i<buttons.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];
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
            BuildBootMenu(menu);
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
