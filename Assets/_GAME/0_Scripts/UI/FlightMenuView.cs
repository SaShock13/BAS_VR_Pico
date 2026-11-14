using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class FlightMenuView : MonoBehaviour
{
    [SerializeField] private TMP_Text pauseLabel;
    private IEventBus _bus;
    private IDisposable _pauseSubscription;
    private GameObject[] _mainButtons;
    private RectTransform _hudPage;

    [Inject]
    public void Construct(IEventBus bus)
    {
        _bus = bus;
        _pauseSubscription = bus.Subscribe<PauseStateChangedEvent>(e =>
            pauseLabel.text = e.IsPaused ? "Продолжить" : "Пауза");
    }

    public void TogglePause() => _bus.Publish(new ToggleFlightPauseRequest());
    public void ReturnToAssembly() => _bus.Publish(new ReturnToAssemblyRequest());

    private void Start()
    {
        var pauseButton = pauseLabel.GetComponentInParent<Button>();
        var canvas = (RectTransform)pauseButton.transform.parent;
        var buttons = canvas.GetComponentsInChildren<Button>(true);
        _mainButtons = new GameObject[buttons.Length + 1];
        for (int i = 0; i < buttons.Length; i++)
            _mainButtons[i] = buttons[i].gameObject;

        _mainButtons[buttons.Length] = CreateButton(canvas, "Настройки HUD", -210f, () => ShowHudPage(true));
        _hudPage = CreateRect(canvas, "HUD Settings", Vector2.zero, new Vector2(800f, 630f));
        for (int i = 0; i < FlightHud.FieldNames.Length; i++)
            CreateHudToggle(i);
        CreateButton(_hudPage, "Назад", -275f, () => ShowHudPage(false));
        ShowHudPage(false);
    }

    private void OnDisable()
    {
        if (_hudPage != null) ShowHudPage(false);
    }

    private void ShowHudPage(bool show)
    {
        foreach (var button in _mainButtons) button.SetActive(!show);
        _hudPage.gameObject.SetActive(show);
    }

    private void CreateHudToggle(int index)
    {
        string label = FlightHud.FieldNames[index];
        var row = CreateRect(_hudPage, label, new Vector2(0f, 270f - index * 61f), new Vector2(800f, 54f));
        var background = row.gameObject.AddComponent<Image>();
        background.color = new Color(0.09f, 0.13f, 0.17f, 0.98f);
        var box = CreateRect(row, "Checkbox", new Vector2(-350f, 0f), new Vector2(38f, 38f));
        var boxImage = box.gameObject.AddComponent<Image>();
        boxImage.color = new Color(0.23f, 0.32f, 0.4f);
        boxImage.raycastTarget = false;
        var check = CreateLabel(box, "✓", Vector2.zero, new Vector2(38f, 38f));
        if (!check.font.HasCharacter('✓')) check.text = "X";
        CreateLabel(row, label, new Vector2(30f, 0f), new Vector2(680f, 60f),
            TextAlignmentOptions.MidlineLeft);
        var toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = check;
        toggle.SetIsOnWithoutNotify(FlightSession.IsHudFieldVisible(index));
        toggle.onValueChanged.AddListener(visible => FlightSession.SetHudFieldVisible(index, visible));
    }

    private GameObject CreateButton(RectTransform parent, string label, float y, Action clicked)
    {
        var rect = CreateRect(parent, label, new Vector2(0f, y), new Vector2(800f, 90f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.23f, 0.3f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => clicked());
        CreateLabel(rect, label, Vector2.zero, rect.sizeDelta);
        return rect.gameObject;
    }

    private TMP_Text CreateLabel(RectTransform parent, string text, Vector2 position, Vector2 size,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = CreateRect(parent, "Label", position, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = pauseLabel.font;
        label.fontSize = 34f;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private static RectTransform CreateRect(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void OnDestroy() => _pauseSubscription?.Dispose();
}
