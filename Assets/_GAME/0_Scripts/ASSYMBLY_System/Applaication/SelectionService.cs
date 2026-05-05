using System;
using UnityEngine;

public class SelectionService
{
    public string SelectedPartId { get; private set; }

    private readonly IEventBus _eventBus;

    public SelectionService(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<Clean_PartDeletedEvent>(OnPartDeleted);
    }

    private void OnPartDeleted(Clean_PartDeletedEvent @event)
    {
        Clear();
    }

    public void Select(string instanceId)
    {
        if (SelectedPartId == instanceId)
            return;

        SelectedPartId = instanceId;

        _eventBus.Publish(new PartSelectedEvent(instanceId));

        Debug.Log($"SelectionService Select {SelectedPartId}");
    }

    public void Clear()
    {
        if (SelectedPartId == null)
            return;

        SelectedPartId = null;
        _eventBus.Publish(new PartDeselectedEvent());
    }
}
