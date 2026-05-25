using System;
using UnityEngine;

public class SelectionService
{
    public string SelectedPartId { get; private set; }
    public string SelectedDroneId { get; private set; }


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

    public void Select(string instanceId, string droneId = null)
    {
        if (SelectedPartId == instanceId)
            return;

        SelectedPartId = instanceId;
        
        SelectedDroneId = droneId;

        _eventBus.Publish(new PartSelectedEvent(instanceId));
        _eventBus.Publish(new DroneSelectedEvent(droneId));


        Debug.Log($"--- SelectionService Select part {SelectedPartId} on Drone {SelectedDroneId}");
    }

    public void Clear()
    {
        if (SelectedPartId == null)
            return;

        SelectedPartId = null;
        _eventBus.Publish(new PartDeselectedEvent());
    }
}
