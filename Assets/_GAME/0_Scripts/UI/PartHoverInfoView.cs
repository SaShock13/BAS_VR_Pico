using TMPro;
using UnityEngine;

public sealed class PartHoverInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text title, description, characteristics, additionalInfo;
    [SerializeField, Min(0)] private float verticalGap = .04f;
    [SerializeField, Range(.75f, 3f)] private float widthInMenuCells = 2.025f;
    private Transform _anchor, _viewer;

    public void Show(PartConfig config, Transform previewAnchor, Transform viewer)
    {
        _anchor = previewAnchor; _viewer = viewer;
        title.text = PartInfoText.Title(config);
        description.text = config.Description ?? string.Empty;
        characteristics.text = PartInfoText.Characteristics(config);
        additionalInfo.text = config.AdditionalInfo ?? string.Empty;
        float y = 20;
        foreach (var label in new[] { title, description, characteristics, additionalInfo })
        {
            bool visible = !string.IsNullOrWhiteSpace(label.text);
            label.gameObject.SetActive(visible);
            if (!visible) continue;
            var rect = label.rectTransform;
            float height = label.GetPreferredValues(label.text, 380, 0).y;
            float maxHeight = label == title ? 90 : label == characteristics ? 260 : 140;
            height = Mathf.Min(height, maxHeight);
            label.overflowMode = TextOverflowModes.Ellipsis;
            // The background writes depth. Keep glyphs in front of that plane
            // so stereo projection/rounding cannot make them fight its depth.
            rect.anchoredPosition3D = new Vector3(20, -y, -1);
            rect.sizeDelta = new Vector2(380, height);
            y += height + 12;
        }
        ((RectTransform)transform).sizeDelta = new Vector2(420, y + 8);
        gameObject.SetActive(true);
        Follow();
    }

    public void Hide() { _anchor = null; gameObject.SetActive(false); }

    // Only one pooled card follows the menu while visible.
    private void LateUpdate() => Follow();

    private void Follow()
    {
        if (_viewer == null || _anchor == null || !_anchor.gameObject.activeInHierarchy) { Hide(); return; }
        var rect = _anchor as RectTransform;
        if (rect != null)
            transform.localScale = Vector3.one * (rect.TransformVector(Vector3.right * rect.rect.width).magnitude * widthInMenuCells / 420f);
        var position = rect != null ? rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMax, 0)) : _anchor.position;
        position += Vector3.up * verticalGap;
        var forward = position - _viewer.position;
        if (forward.sqrMagnitude > .000001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.position = position;
    }
}
