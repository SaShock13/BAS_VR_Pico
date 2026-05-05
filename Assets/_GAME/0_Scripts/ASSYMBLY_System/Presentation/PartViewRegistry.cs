
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PartViewRegistry : IInitializable
{
    private readonly IEventBus _eventBus;

    private readonly Dictionary<string, GameObject> _views =
        new Dictionary<string, GameObject>();
    
    private DronePartView _visualSelectedView;

    public PartViewRegistry(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Initialize()
    {

        Debug.Log($"Clean_PartViewRegistry Initialize {this}");
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _eventBus.Subscribe<Clean_PartDeletedEvent>(OnPartDeleted);
        _eventBus.Subscribe<PartVisualChangedEvent>(OnPartVisualChanged);
        
    }      


    private void OnPartVisualChanged(PartVisualChangedEvent @event)
    {
        if (_views.TryGetValue(@event.InstanceId, out var go))
        {
            var view = go.GetComponent<DronePartView>();

            Debug.Log($"view {view!= null}");
            view.ApplyVisualCommitted(@event.Visual);
        }
    }

    private void OnPartDeleted(Clean_PartDeletedEvent @event)
    {
        if (_views.TryGetValue(@event.InstanceId, out GameObject go))
        {
            GameObject.Destroy(go);
            _views.Remove(@event.InstanceId);
        }
    }

    private void OnPartCreated(Clean_PartCreatedEvent @event)
    {
        //_views[@event.InstanceId] = @event.GameObject;

        Register(@event.InstanceId, @event.GameObject);

        Debug.Log($"Added Part with ID  {@event.InstanceId}");
    }




    public bool TryGet(string partId, out DronePartView view)
    {
        _views.TryGetValue(partId, out var go);
        return view = go.GetComponent<DronePartView>();
    }



    
    public void VisualSelect(string InstanceId)
    {
        // Снять подсветку с предыдущей
        VisualDeselectAll();

        if (_views.TryGetValue(InstanceId, out var go))
        {
            var view = go.GetComponent<DronePartView>();
            view.VisualSelection(true);
            _visualSelectedView = view;
        }
    }

    public void VisualDeselectAll()
    {
        if (_visualSelectedView != null)
        {   
            // снять визуальное выделение ? Может и не надо снимать выделение?
            _visualSelectedView = null;
        }
    }

    public void Register(string instanceId, GameObject view)
    {
        _views[instanceId] = view;
    }

    internal IEnumerable<GameObject> GetAllGOs()
    {
        return _views.Values;
    }

    internal void Clear()
    {
        _views.Clear();
    }
}
