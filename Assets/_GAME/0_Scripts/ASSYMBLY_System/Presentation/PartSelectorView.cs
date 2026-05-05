using System;
using UnityEngine;
using Zenject;

public class PartSelectorView : MonoBehaviour
{
    private PartViewRegistry _registry;
    private IEventBus _eventBus;


    [Inject]
    public void Construct(IEventBus eventBus, PartViewRegistry registry)
    {
        _eventBus = eventBus;
        _registry = registry;
        _eventBus.Subscribe<PartSelectedEvent>(OnPartSelected);
        _eventBus.Subscribe<PartDeselectedEvent>(OnPartDeselected);
    }

    private void OnPartDeselected(PartDeselectedEvent @event)
    {
        _registry.VisualDeselectAll();
    }

    private void OnPartSelected(PartSelectedEvent @event)
    {
        _registry.VisualSelect(@event.InstanceId );
    }

}
