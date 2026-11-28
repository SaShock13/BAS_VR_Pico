using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[DisallowMultipleComponent]
public sealed class MenuTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, TextArea] private string tooltipText;
    [SerializeField, Min(0f)] private float hoverDelay = 1f;

    private readonly HashSet<int> _hoveringPointers = new();
    private Coroutine _pending;
    private MenuTooltipPanel _panel;

    public void Configure(string text, float delay = 1f)
    {
        tooltipText = text;
        hoverDelay = Mathf.Max(0f, delay);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled ||
            eventData is not TrackedDeviceEventData { interactor: XRRayInteractor or NearFarInteractor } ||
            string.IsNullOrWhiteSpace(tooltipText))
            return;
        BeginHover(eventData.pointerId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EndHover(eventData.pointerId);
    }

    public void BeginHover(int pointerId)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(tooltipText) ||
            !_hoveringPointers.Add(pointerId) || _hoveringPointers.Count != 1) return;
        _pending = StartCoroutine(ShowAfterDelay());
    }

    public void EndHover(int pointerId)
    {
        if (_hoveringPointers.Remove(pointerId) && _hoveringPointers.Count == 0)
            Hide();
    }

    public void CancelHover()
    {
        _hoveringPointers.Clear();
        Hide();
    }

    private void OnDisable() => CancelHover();

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(hoverDelay);
        _pending = null;

        var canvas = GetComponentInParent<Canvas>();
        if (!canvas)
            yield break;

        var root = canvas.rootCanvas;
        if (!root.TryGetComponent(out _panel))
            _panel = root.gameObject.AddComponent<MenuTooltipPanel>();

        _panel.Show(this, (RectTransform)transform, tooltipText);
    }

    private void Hide()
    {
        if (_pending != null)
        {
            StopCoroutine(_pending);
            _pending = null;
        }

        if (_panel)
            _panel.Hide(this);
    }
}
