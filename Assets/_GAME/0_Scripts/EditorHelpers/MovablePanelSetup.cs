#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class MovablePanelSetup
{
    [MenuItem("Tools/BAS VR/Set Up Movable Inspector Panels")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/ConstructorScene.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ConstructorScene.unity", OpenSceneMode.Additive);
        var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var graphic in objects.Select(x => x.GetComponent<Graphic>()).Where(x => x != null).ToArray())
            if (graphic.GetComponent<CanvasRenderer>() == null)
                Undo.AddComponent<CanvasRenderer>(graphic.gameObject);
        var mainCamera = objects.Select(x => x.GetComponent<Camera>()).First(x => x != null && x.CompareTag("MainCamera"));
        int panels = 0, tips = 0;
        foreach (var raycaster in objects.Select(x => x.GetComponent<CurvedScreenRaycaster>()).Where(x => x != null).ToArray())
        {
            var canvas = (RectTransform)raycaster.transform;
            if (canvas.rect.width <= 0) continue;
            var panel = canvas.parent.parent;
            if (!panel.name.StartsWith("Curved Part Screen") && !panel.name.StartsWith("Curved Drone Screen")) continue;
            AddGrip(panel, mainCamera);
            panels++;
            foreach (var selectable in canvas.GetComponentsInChildren<Selectable>(true))
            {
                // Dropdown-generated template items use the dropdown's hint.
                if (selectable.GetComponentInParent<ScrollRect>() != null) continue;
                var text = TooltipFor(selectable);
                if (string.IsNullOrEmpty(text)) continue;
                AddTooltip(selectable.gameObject, text);
                tips++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Movable panels: {panels}; control tooltips: {tips}");
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    private static void AddGrip(Transform panel, Camera mainCamera)
    {
        if (panel.GetComponent<MovablePanelView>() != null)
        {
            ConfigureBarTooltip(panel.Find("Panel grip bar").gameObject);
            UpdateCurvedGrip(panel);
            return;
        }
        var surface = panel.Find("Surface").GetComponent<MeshFilter>();
        var bounds = surface.sharedMesh.bounds;
        var body = Undo.AddComponent<Rigidbody>(panel.gameObject);
        body.useGravity = false;
        body.isKinematic = true;
        var grab = Undo.AddComponent<XRGrabInteractable>(panel.gameObject);
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = false;
        grab.useDynamicAttach = true;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.snapToColliderVolume = true;
        grab.attachEaseInTime = 0;
        grab.trackScale = false;
        grab.selectMode = InteractableSelectMode.Single;
        grab.retainTransformParent = true;
        var view = Undo.AddComponent<MovablePanelView>(panel.gameObject);
        var binding = new SerializedObject(view);
        binding.FindProperty("grab").objectReferenceValue = grab;
        binding.FindProperty("body").objectReferenceValue = body;
        binding.FindProperty("defaultParent").objectReferenceValue = panel.parent;
        binding.FindProperty("defaultPosition").vector3Value = panel.localPosition;
        binding.FindProperty("defaultRotation").quaternionValue = panel.localRotation;
        binding.FindProperty("defaultScale").vector3Value = panel.localScale;
        binding.ApplyModifiedProperties();

        float width = bounds.size.x * surface.transform.lossyScale.x * 1000;
        var bar = Rect("Panel grip bar", panel, Vector2.zero, new Vector2(width, 82));
        var scale = panel.lossyScale;
        bar.localScale = new Vector3(.001f / scale.x, .001f / scale.y, .001f / scale.z);
        bar.position = surface.transform.TransformPoint(new Vector3(bounds.center.x,
            bounds.min.y - .055f / surface.transform.lossyScale.y,
            bounds.min.z - .02f / surface.transform.lossyScale.z));
        var canvas = bar.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        bar.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        ConfigureBarTooltip(bar.gameObject);
        var handle = Rect("Grab strip", bar, new Vector2(-46, 0), new Vector2(width - 98, 82));
        var image = handle.gameObject.AddComponent<Image>();
        image.color = new Color(.09f, .17f, .21f, 1);
        image.raycastTarget = false;
        Label(handle, "ЗАХВАТИТЕ, ЧТОБЫ ПЕРЕМЕСТИТЬ", 23);
        var collider = handle.gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(width - 98, 82, 45);
        // Only the grip participates in XR selection, never the UI surface.
        grab.colliders.Clear();
        grab.colliders.Add(collider);
        var reset = Rect("Reset panel position", bar, new Vector2(width * .5f - 41, 0), new Vector2(82, 82));
        image = reset.gameObject.AddComponent<Image>(); image.color = new Color(.16f, .25f, .29f, 1);
        var button = reset.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, view.ResetPosition);
        var icon = Rect("Reset icon", reset, Vector2.zero, new Vector2(65, 65));
        var graphic = icon.gameObject.AddComponent<PanelResetIcon>();
        graphic.color = new Color(.95f, .75f, .32f, 1);
        graphic.raycastTarget = false;
        AddTooltip(reset.gameObject, "Вернуть панель на место");
        EditorUtility.SetDirty(grab);
        UpdateCurvedGrip(panel);
    }

    private static void UpdateCurvedGrip(Transform panel)
    {
        var surface = panel.Find("Surface").GetComponent<MeshFilter>();
        var mesh = surface.sharedMesh;
        var bounds = mesh.bounds;
        var edge = mesh.vertices.Where(v => Mathf.Abs(v.y - bounds.min.y) < .001f)
            .GroupBy(v => v.x).Select(g => g.First()).OrderBy(v => v.x).ToArray();
        if (edge.Length < 2) throw new InvalidOperationException("Panel bottom edge is missing");
        var bar = (RectTransform)panel.Find("Panel grip bar");
        Undo.RecordObject(bar, "Curve and narrow panel grip");
        float width = bounds.size.x * surface.transform.lossyScale.x * 500f;
        const float height = 82f * 2f / 3f;
        bar.sizeDelta = new Vector2(width, height);
        bar.position = surface.transform.TransformPoint(new Vector3(bounds.center.x,
            bounds.min.y - .055f / surface.transform.lossyScale.y,
            -.02f / surface.transform.lossyScale.z));
        var handle = (RectTransform)bar.Find("Grab strip");
        Undo.RecordObject(handle, "Update curved grip bounds");
        handle.anchoredPosition = Vector2.zero;
        handle.sizeDelta = new Vector2(width, height);
        var image = handle.GetComponent<Image>();
        if (image != null) Undo.DestroyObjectImmediate(image);
        var graphic = handle.GetComponent<CurvedGripGraphic>();
        if (handle.GetComponent<CanvasRenderer>() == null) Undo.AddComponent<CanvasRenderer>(handle.gameObject);
        if (graphic == null) graphic = Undo.AddComponent<CurvedGripGraphic>(handle.gameObject);
        graphic.color = new Color(.09f, .17f, .21f, 1);
        graphic.raycastTarget = false;
        var oldCollider = handle.GetComponent<BoxCollider>();
        if (oldCollider != null) Undo.DestroyObjectImmediate(oldCollider);
        foreach (var child in handle.Cast<Transform>().Where(t => t.name.StartsWith("Grip collider ")).ToArray())
            Undo.DestroyObjectImmediate(child.gameObject);
        var grab = panel.GetComponent<XRGrabInteractable>();
        Undo.RecordObject(grab, "Match XR grip to curved strip");
        grab.colliders.Clear();

        Vector3 Point(float pixelX)
        {
            float x = bounds.center.x + pixelX / (1000f * surface.transform.lossyScale.x);
            int index = 1;
            while (index < edge.Length - 1 && edge[index].x < x) index++;
            var a = edge[index - 1]; var b = edge[index];
            float z = Mathf.Lerp(a.z, b.z, Mathf.InverseLerp(a.x, b.x, x));
            var world = surface.transform.TransformPoint(new Vector3(x,
                bounds.min.y - .055f / surface.transform.lossyScale.y,
                z - .02f / surface.transform.lossyScale.z));
            return handle.InverseTransformPoint(world);
        }

        const int segments = 8;
        float startX = -width * .5f, endX = width * .5f - height - 16;
        // Preserve every bend in the source mesh rather than approximating its arc.
        var points = edge.Select(v => (v.x - bounds.center.x) * 1000f * surface.transform.lossyScale.x)
            .Where(x => x > startX && x < endX).Concat(new[] { startX, endX }).OrderBy(x => x)
            .Select(Point).ToArray();
        var binding = new SerializedObject(graphic);
        var property = binding.FindProperty("points");
        property.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) property.GetArrayElementAtIndex(i).vector3Value = points[i];
        binding.ApplyModifiedProperties();
        graphic.SetVerticesDirty();
        for (int i = 0; i < segments; i++)
        {
            var from = Point(Mathf.Lerp(startX, endX, i / (float)segments));
            var to = Point(Mathf.Lerp(startX, endX, (i + 1) / (float)segments));
            var go = new GameObject("Grip collider " + i);
            Undo.RegisterCreatedObjectUndo(go, "Add curved grip collider");
            go.transform.SetParent(handle, false);
            go.transform.localPosition = (from + to) * .5f;
            var direction = to - from;
            go.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(direction.normalized, Vector3.up), Vector3.up);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(direction.magnitude + 1, height, 45);
            grab.colliders.Add(collider);
        }
        var label = (RectTransform)handle.Find("Label");
        Undo.RecordObject(label, "Fit grip label");
        label.sizeDelta = new Vector2((width - height - 16) * .9f, height * .9f);
        label.anchoredPosition3D = Point(-(height + 16) * .5f) + Vector3.back * 20;
        var curvedText = label.GetComponent<CurvedPanelText>();
        if (curvedText != null) Undo.DestroyObjectImmediate(curvedText);
        var textLabel = label.GetComponent<TextMeshProUGUI>();
        Undo.RecordObject(textLabel, "Remove permanent grip caption");
        textLabel.text = string.Empty;
        AddTooltip(handle.gameObject, "Захватите, чтобы переместить");
        var viewBinding = new SerializedObject(panel.GetComponent<MovablePanelView>());
        viewBinding.FindProperty("gripTooltip").objectReferenceValue = handle.GetComponent<MenuTooltip>();
        viewBinding.ApplyModifiedProperties();
        var reset = (RectTransform)bar.Find("Reset panel position");
        Undo.RecordObject(reset, "Align reset button with curved grip");
        reset.sizeDelta = new Vector2(height, height);
        reset.localPosition = Point(width * .5f - height * .5f);
        var tangent = Point(width * .5f) - Point(width * .5f - height);
        reset.localRotation = Quaternion.LookRotation(Vector3.Cross(tangent.normalized, Vector3.up), Vector3.up);
        var resetIcon = (RectTransform)reset.Find("Reset icon");
        Undo.RecordObject(resetIcon, "Fit reset icon to grip height");
        resetIcon.sizeDelta = Vector2.one * height * .8f;
        EditorUtility.SetDirty(grab);
    }

    private static void ConfigureBarTooltip(GameObject bar)
    {
        var panel = bar.GetComponent<MenuTooltipPanel>();
        if (panel == null) panel = Undo.AddComponent<MenuTooltipPanel>(bar);
        var binding = new SerializedObject(panel);
        binding.FindProperty("keepInsideCanvas").boolValue = false;
        binding.ApplyModifiedProperties();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add movable panel controls");
        // Offscreen UI cameras render layer UI only; this strip is physical world UI.
        go.layer = 0;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Label(RectTransform parent, string text, float size)
    {
        var rect = Rect("Label", parent, Vector2.zero, parent.sizeDelta * .9f);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_GAME/Art/MainMenu/AssemblyProjectFont.asset");
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private static void AddTooltip(GameObject target, string text)
    {
        var tooltip = target.GetComponent<MenuTooltip>();
        if (tooltip == null) tooltip = Undo.AddComponent<MenuTooltip>(target);
        Undo.RecordObject(tooltip, "Configure panel tooltip");
        tooltip.Configure(text, .7f);
        EditorUtility.SetDirty(tooltip);
    }

    private static string TooltipFor(Selectable control)
    {
        switch (control.name)
        {
            case "PresetDropdown": return "Применить сохранённый вид";
            case "DeletePresetButton": return "Удалить выбранный пресет";
            case "MaterialDropdown": return "Выбрать материал детали";
            case "ColorButton": return "Выбрать цвет детали";
            case "SmoothnessSlider": return "Настроить гладкость поверхности";
            case "MetallnesSlider": return "Настроить металлический блеск";
            case "SavePresetButton": return "Сохранить материал и цвет";
            case "DeletePartButton": return "Удалить выбранную деталь";
            case "DuplicatePartButton": return "Создать копию детали";
            case "ValidateButton": return "Проверить готовность к полёту";
            case "SendToGarage": return "Сохранить дрон в гараже";
            case "ArcadeToggle": return "Сменить режим управления полётом";
            case "LaunchFlight": return "Выбрать миссию и начать полёт";
            case "Previous": return "Предыдущая миссия";
            case "Next": return "Следующая миссия";
            case "Cancel": return "Отменить действие";
            case "Confirm": return control.transform.parent.name == "Dialog" ? "Подтвердить удаление" : "Начать выбранную миссию";
            case "UniformScaleButton": return "Изменять все размеры вместе";
            case "MotorUniformMinus": return "Уменьшить размер на 5%";
            case "MotorUniformPlus": return "Увеличить размер на 5%";
            case "Сброс сдвига": return "Вернуть исходный сдвиг";
            case "Сброс размера": return "Вернуть исходный размер";
            case "Ручки: выкл.": return "Показать ручки изменения размера";
            case "Размер / Материал": return "Переключить настройки детали";
        }
        if (control is TMP_InputField) return control.transform.parent.name == "EnterNamePanel" ? "Введите имя пресета" : "Введите имя дрона";
        if (control.name.StartsWith("Длина")) return "Изменить длину детали";
        if (control.name.StartsWith("Высота")) return "Изменить высоту детали";
        if (control.name.StartsWith("Ширина")) return "Изменить ширину детали";
        if (control.name.StartsWith("Вдоль")) return "Сдвинуть деталь на 1 мм";
        if (control is Button button)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                var method = button.onClick.GetPersistentMethodName(i);
                if (method == "ResetRotation") return "Сбросить поворот детали";
                if (method == "ResetPosition") return "Сбросить сдвиг детали";
                if (method.StartsWith("Move") && method.Length > 4)
                    return "Сдвиг по " + method[4] + (method.EndsWith("Plus") ? ": +5 см" : ": −5 см");
                if (method.StartsWith("Rotate") && method.Length > 6)
                    return "Поворот по " + method[6] + (method.EndsWith("Plus") ? ": +1°" : ": −1°");
            }
            if (control.transform.parent.name == "DronePanel") return "Применить новое имя дрона";
            if (control.transform.parent.name == "EnterNamePanel") return "Сохранить пресет под этим именем";
        }
        return null;
    }
}
#endif
