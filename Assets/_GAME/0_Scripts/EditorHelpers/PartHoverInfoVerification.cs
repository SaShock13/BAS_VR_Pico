#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Zenject;

public static class PartHoverInfoVerification
{
    private sealed class Logger : IAppLogger
    {
        public bool IsDebugEnabled => false;
        public void Initialize() { }
        public void Log(string text) { }
        public void LogWarning(string text) { }
        public void LogError(string text) { }
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode check only");
        var scene = EditorSceneManager.NewPreviewScene();
        var prefab = AssetDatabase.LoadAssetAtPath<PartHoverInfoView>(PartHoverInfoSetup.PrefabPath);
        var small = AssetDatabase.LoadAssetAtPath<PartConfig>("Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/Tech/FlyghtControllerConfig.asset");
        var large = AssetDatabase.LoadAssetAtPath<PartConfig>("Assets/_GAME/0_Scripts/ASSYMBLY_System/DOMAIN/Parts/NewParts/Tech/FlyghtController_30_5Config.asset");
        var bus = new EventBus(new Logger(), new DiContainer());
        PartHoverInfoPresenter presenter = null;
        PartHoverInfoView view = null;
        try
        {
            bus.Initialize();
            var viewer = new GameObject("Test viewer").transform;
            SceneManager.MoveGameObjectToScene(viewer.gameObject, scene);
            viewer.position = new Vector3(0, 1, 0);
            RectTransform Anchor(string name, Vector3 position)
            {
                var go = new GameObject(name, typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(go, scene);
                var rect = (RectTransform)go.transform;
                rect.position = position; rect.localScale = Vector3.one * .001f; rect.sizeDelta = Vector2.one * 210;
                return rect;
            }
            var a = Anchor("Cell A", new Vector3(-.2f, 1, 1));
            var b = Anchor("Cell B", new Vector3(.2f, 1, 1));
            presenter = new PartHoverInfoPresenter(bus, new PartConfigRepository(new[] { small, large }), prefab, viewer, scene);
            presenter.Initialize();
            view = scene.GetRootGameObjects().Select(x => x.GetComponent<PartHoverInfoView>()).Single(x => x != null);
            var title = view.GetComponentsInChildren<TMP_Text>(true).First(x => x.name == "Title");
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
            Check(!view.gameObject.activeSelf, "Card visible before hover");
            bus.Publish(new PartInfoHoverEvent { Source = a, IsHovered = true, ConfigId = small.PartId, PreviewAnchor = a });
            Check(view.gameObject.activeSelf && title.text == PartInfoText.Title(small), "First hover not shown");
            float cardWidth = ((RectTransform)view.transform).rect.width * view.transform.lossyScale.x;
            Check(Mathf.Abs(cardWidth - .42525f) < .001f, "Card does not scale relative to menu cell");
            bus.Publish(new PartInfoHoverEvent { Source = b, IsHovered = true, ConfigId = large.PartId, PreviewAnchor = b });
            Check(title.text == PartInfoText.Title(large), "Latest hover not shown");
            bus.Publish(new PartInfoHoverEvent { Source = b, IsHovered = false });
            Check(title.text == PartInfoText.Title(small) && view.gameObject.activeSelf, "Two-controller fallback failed");
            bus.Publish(new PartInfoHoverEvent { Source = a, IsHovered = false });
            Check(!view.gameObject.activeSelf, "Card persists after exit");
            var hover = a.gameObject.AddComponent<PartInfoPreviewHover>(); hover.Configure(small, bus);
            var first = new TrackedDeviceEventData(null) { pointerId = 10 };
            var second = new TrackedDeviceEventData(null) { pointerId = 11 };
            hover.OnPointerEnter(first); hover.OnPointerEnter(second); hover.OnPointerExit(first);
            Check(view.gameObject.activeSelf, "First hand exit hid second hand's card");
            hover.OnPointerExit(second);
            Check(!view.gameObject.activeSelf, "Last pointer exit did not hide card");
            foreach (var graphic in view.GetComponentsInChildren<Graphic>(true))
                Check(!graphic.raycastTarget && graphic.GetComponent<CanvasRenderer>() != null, "Card blocks UI or lacks CanvasRenderer");
            Check(view.GetComponentsInChildren<Camera>(true).Length == 0 && view.GetComponentsInChildren<BaseRaycaster>(true).Length == 0,
                "Card has an extra camera or raycaster");
            Check(PartInfoText.Characteristics(small).Contains("Крепление: 20×20 мм"), "Editable characteristics not displayed");
            Check(!string.IsNullOrWhiteSpace(small.Description), "Description not editable/populated");
            Debug.Log("PASS Part hover info: config text, cell-relative size, two-controller fallback, pointer exit, non-blocking prefab.");
        }
        finally
        {
            if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
            presenter?.Dispose(); bus.Dispose(); EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
