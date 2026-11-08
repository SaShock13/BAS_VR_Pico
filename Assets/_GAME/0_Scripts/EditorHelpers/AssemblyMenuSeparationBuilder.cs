#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class AssemblyMenuSeparationBuilder
{
    [MenuItem("Tools/BAS VR/Separate Assembly Menus")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ConstructorScene.unity") throw new InvalidOperationException("Open ConstructorScene");
        var controller = UnityEngine.Object.FindFirstObjectByType<LeftHandMenu>();
        var binding = new SerializedObject(controller);
        var menu = (GameObject)binding.FindProperty("sceneMenu").objectReferenceValue;
        var partsCanvas = menu.GetComponentInChildren<Canvas>(true);
        var anchor = (Transform)binding.FindProperty("menuTransform").objectReferenceValue;
        var system = (GameObject)binding.FindProperty("systemMenu").objectReferenceValue;
        if (system == null)
        {
            system = new GameObject("AssemblySystemMenu", typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(system, "Separate assembly menus");
            system.transform.SetParent(anchor, false);
        }
        var canvasTransform = system.transform.Find("Canvas") as RectTransform;
        if (canvasTransform == null)
        {
            canvasTransform = Rect("Canvas", system.transform, new Vector2(1100, 650), Vector2.zero);
            var canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasTransform.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasTransform.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .06f, 1);
            canvasTransform.localScale = partsCanvas.transform.localScale;
            var title = Rect("Title", canvasTransform, new Vector2(1000, 90), new Vector2(0, 240));
            var text = title.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
            text.text = "Меню сборки"; text.fontSize = 42; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        }
        var saves = partsCanvas.transform.Find("ProjectSaveControls") ?? canvasTransform.Find("ProjectSaveControls");
        if (saves == null) throw new InvalidOperationException("ProjectSaveControls is missing");
        Undo.SetTransformParent(saves, canvasTransform, "Move save actions to system menu");
        saves.localPosition = Vector3.zero; saves.localRotation = Quaternion.identity; saves.localScale = Vector3.one;
        SetLabel(saves.Find("Rename"), "Переименовать");
        var rename = saves.Find("Rename") as RectTransform;
        rename.sizeDelta = new Vector2(300, 58); rename.anchoredPosition = new Vector2(360,115);
        var input = saves.Find("ProjectName") as RectTransform;
        input.sizeDelta = new Vector2(650,58); input.anchoredPosition = new Vector2(-160,115);
        SetLabel(saves.Find("Save"), "Сохранить сборку");
        SetLabel(saves.Find("SaveExit"), "Сохранить и выйти");
        SetLabel(saves.Find("Discard"), "Выйти без сохранения…");
        var close = canvasTransform.Find("Close") as RectTransform;
        if (close == null)
        {
            close = Rect("Close", canvasTransform, new Vector2(600, 65), new Vector2(0,-245));
            var image = close.gameObject.AddComponent<Image>(); image.color = new Color(.08f,.16f,.2f,1);
            var button = close.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Rect("Label",close,new Vector2(580,60),Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
            label.text="Закрыть"; label.fontSize=30; label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false;
            UnityEventTools.AddPersistentListener(button.onClick,controller.HideMenu);
        }
        var droneUI = UnityEngine.Object.FindFirstObjectByType<DronePanelUI>();
        var droneBinding = new SerializedObject(droneUI);
        var dronePanel = (GameObject)droneBinding.FindProperty("panel").objectReferenceValue;
        var flight = partsCanvas.transform.Find("FlightControls") ?? dronePanel.transform.Find("FlightControls");
        if (flight == null || flight.GetComponent<FlightModeView>() == null) throw new InvalidOperationException("FlightControls is missing");
        Undo.SetTransformParent(flight, dronePanel.transform, "Move flight actions to drone panel");
        var flightRect = (RectTransform)flight;
        flightRect.anchorMin=flightRect.anchorMax=new Vector2(0,1);
        flightRect.pivot=new Vector2(.5f,.5f);
        flightRect.anchoredPosition3D=new Vector3(5,-8.65f,0);
        flightRect.localRotation=Quaternion.identity; flightRect.localScale=Vector3.one*.0086f;
        foreach (var child in new[]{"ArcadeToggle","LaunchFlight"})
        {
            var rect=(RectTransform)flight.Find(child);
            rect.sizeDelta=new Vector2(520,100);
            rect.anchoredPosition=new Vector2(child=="ArcadeToggle" ? -275:275,0);
            var label=rect.Find("Label") as RectTransform;
            label.sizeDelta=new Vector2(490,92);
            label.GetComponent<TMP_Text>().fontSize=36;
        }
        var panelRect=(RectTransform)dronePanel.transform;
        var statsRect=(RectTransform)droneUI.transform;
        statsRect.sizeDelta=new Vector2(statsRect.sizeDelta.x,Mathf.Max(11,statsRect.sizeDelta.y));
        panelRect.sizeDelta=new Vector2(panelRect.sizeDelta.x,0);
        var renameDrone=(Button)droneBinding.FindProperty("renameButton").objectReferenceValue;
        var renameRect=(RectTransform)renameDrone.transform;
        renameRect.anchorMin=new Vector2(0,1); renameRect.anchorMax=new Vector2(1,1);
        renameRect.pivot=new Vector2(.5f,1); renameRect.anchoredPosition=new Vector2(0,-9.6f);
        var partsRect=(RectTransform)partsCanvas.transform;
        partsRect.sizeDelta=new Vector2(partsRect.sizeDelta.x,650);
        binding.FindProperty("systemMenu").objectReferenceValue=system;
        binding.FindProperty("longPressDuration").floatValue=1;
        binding.ApplyModifiedProperties();
        system.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Separated assembly menus; flight controls moved to StatsCanvas/DronePanel.");
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=5;
        var rect=(RectTransform)go.transform; rect.SetParent(parent,false);
        rect.sizeDelta=size; rect.anchoredPosition=position; return rect;
    }
    private static void SetLabel(Transform target,string text)
    {
        var label=target.GetComponentInChildren<TMP_Text>(true);
        label.text=text;
        label.enableAutoSizing=true; label.fontSizeMin=18; label.fontSizeMax=25;
        var rect=(RectTransform)label.transform;
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(8,4); rect.offsetMax=new Vector2(-8,-4);
    }
}
#endif
