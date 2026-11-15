using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MenuTooltipPanel : MonoBehaviour
{
    private RectTransform _panel;
    private TextMeshProUGUI _label;
    private MenuTooltip _owner;

    public void Show(MenuTooltip owner, RectTransform control, string message)
    {
        if (!_panel)
            CreatePanel(control);

        _owner = owner;
        _label.text = message;

        const float padding = 12f;
        const float maxTextWidth = 320f;
        float textWidth = Mathf.Clamp(_label.GetPreferredValues(message).x, 1f, maxTextWidth);
        float textHeight = _label.GetPreferredValues(message, textWidth, 0f).y;
        _panel.sizeDelta = new Vector2(textWidth + 2f * padding, textHeight + 2f * padding);
        var top = control.TransformPoint(new Vector3(control.rect.center.x, control.rect.yMax, 0f));
        var position = transform.InverseTransformPoint(top);
        position.y += 8f;
        _panel.localPosition = position;
        _panel.SetAsLastSibling();
        _panel.gameObject.SetActive(true);
    }

    public void Hide(MenuTooltip owner)
    {
        if (_owner != owner)
            return;

        _owner = null;
        _panel.gameObject.SetActive(false);
    }

    private void CreatePanel(RectTransform control)
    {
        var panel = new GameObject("Menu tooltip", typeof(RectTransform), typeof(Canvas), typeof(Image));
        _panel = (RectTransform)panel.transform;
        _panel.SetParent(transform, false);
        _panel.anchorMin = _panel.anchorMax = ((RectTransform)transform).pivot;
        _panel.pivot = new Vector2(0.5f, 0f);

        var canvas = panel.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = GetComponent<Canvas>().sortingLayerID;
        canvas.sortingOrder = short.MaxValue;

        var background = panel.GetComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.1f, 0.97f);
        background.raycastTarget = false;

        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        _label = label.GetComponent<TextMeshProUGUI>();
        var source = control.GetComponentInChildren<TMP_Text>();
        if (source)
            _label.font = source.font;

        _label.rectTransform.SetParent(_panel, false);
        _label.rectTransform.anchorMin = Vector2.zero;
        _label.rectTransform.anchorMax = Vector2.one;
        _label.rectTransform.offsetMin = new Vector2(12f, 12f);
        _label.rectTransform.offsetMax = new Vector2(-12f, -12f);
        _label.fontSize = 22f;
        _label.color = Color.white;
        _label.raycastTarget = false;
    }
}
