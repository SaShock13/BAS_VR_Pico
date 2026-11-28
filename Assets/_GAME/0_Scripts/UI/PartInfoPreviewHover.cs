using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Zenject;

public sealed class PartInfoPreviewHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PartConfig config;
    private readonly HashSet<int> _pointers = new HashSet<int>();
    private IEventBus _bus;
    [Inject] public void Construct(IEventBus bus) => _bus = bus;
    public void Configure(PartConfig partConfig, IEventBus bus) { config = partConfig; _bus = bus; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!(eventData is TrackedDeviceEventData) || config == null || _bus == null ||
            !_pointers.Add(eventData.pointerId) || _pointers.Count != 1) return;
        _bus.Publish(new PartInfoHoverEvent { Source = this, ConfigId = config.PartId, PreviewAnchor = transform, IsHovered = true });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pointers.Remove(eventData.pointerId) && _pointers.Count == 0)
            _bus?.Publish(new PartInfoHoverEvent { Source = this, IsHovered = false });
    }

    private void OnDisable()
    {
        if (_pointers.Count > 0) _bus?.Publish(new PartInfoHoverEvent { Source = this, IsHovered = false });
        _pointers.Clear();
    }
}
