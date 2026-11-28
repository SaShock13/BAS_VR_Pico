using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MenuTooltipPanel : MonoBehaviour
{
    [SerializeField] private bool keepInsideCanvas = true;
    private RectTransform _panel;
    private TextMeshProUGUI _label;
    private MenuTooltip _owner;
    private readonly Vector3[] _corners = new Vector3[4];

    public void Show(MenuTooltip owner, RectTransform control, string message)
    {
        if (!_panel)
            CreatePanel(control);

        _owner = owner;
        _label.text = message;

        var source = control.GetComponentInChildren<TMP_Text>();
        _label.fontSize = source
            ? control.InverseTransformVector(source.transform.TransformVector(Vector3.up * source.fontSize)).magnitude * 0.8f
            : control.rect.height * 0.28f;
        if (source) _label.font = source.font;
        float padding = _label.fontSize * 0.35f;
        float maxTextWidth = Mathf.Max(_label.fontSize * 12f, control.rect.width * 0.95f - 2f * padding);
        float textWidth = Mathf.Clamp(_label.GetPreferredValues(message).x, 0.001f, maxTextWidth);
        float textHeight = _label.GetPreferredValues(message, textWidth, 0f).y;
        _panel.sizeDelta = new Vector2(textWidth + 2f * padding, textHeight + 2f * padding);
        _label.rectTransform.offsetMin = Vector2.one * padding;
        _label.rectTransform.offsetMax = -Vector2.one * padding;
        // Measure in the control's units, including nested menu scaling.
        var relativeScale = transform.InverseTransformVector(control.TransformVector(Vector3.right)).magnitude;
        _panel.localScale = Vector3.one * relativeScale;
        _panel.rotation = control.rotation;
        _panel.position = control.TransformPoint(new Vector3(control.rect.center.x,
            control.rect.yMax + control.rect.height * 0.12f, -0.01f));
        if (keepInsideCanvas) KeepInsideCanvas();
        _panel.SetAsLastSibling();
        _panel.gameObject.SetActive(true);
    }

    private void KeepInsideCanvas()
    {
        var bounds = ((RectTransform)transform).rect;
        if (bounds.width <= 0 || bounds.height <= 0) return;
        _panel.GetWorldCorners(_corners);
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in _corners)
        {
            Vector2 point = transform.InverseTransformPoint(corner);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        var offset = new Vector3(
            min.x < bounds.xMin ? bounds.xMin - min.x : max.x > bounds.xMax ? bounds.xMax - max.x : 0,
            min.y < bounds.yMin ? bounds.yMin - min.y : max.y > bounds.yMax ? bounds.yMax - max.y : 0, 0);
        _panel.localPosition += offset;
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
        panel.layer = gameObject.layer;
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
        label.layer = gameObject.layer;
        _label = label.GetComponent<TextMeshProUGUI>();
        var source = control.GetComponentInChildren<TMP_Text>();
        if (!source) source = GetComponentInChildren<TMP_Text>();
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
