
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class PartViewRegistry : IInitializable
{
    private readonly IEventBus _eventBus;

    private readonly Dictionary<string, GameObject> _views =
        new Dictionary<string, GameObject>();
    private readonly Dictionary<string, DronePartView> _partViews =
        new Dictionary<string, DronePartView>();
    
    private DronePartView _visualSelectedView;

    public PartViewRegistry(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Initialize()
    {

        Debug.Log($"Clean_PartViewRegistry Initialize {this}");
        _eventBus.Subscribe<Clean_PartCreatedEvent>(OnPartCreated);
        _eventBus.Subscribe<PartVisualChangedEvent>(OnPartVisualChanged);
        
    }      


    private void OnPartVisualChanged(PartVisualChangedEvent @event)
    {
        if (_views.TryGetValue(@event.InstanceId, out var go))
        {
            var view = go.GetComponent<DronePartView>();

            Debug.Log($"view Visual Changed {view!= null}");
            view.ApplyVisualCommitted(@event.Visual);
        }
    }

    private void OnPartDeleted(Clean_PartDeletedEvent @event)
    {
        if (_views.TryGetValue(@event.InstanceId, out GameObject go))
        {
            _views.Remove(@event.InstanceId);
            _partViews.Remove(@event.InstanceId);
            if (go != null) GameObject.Destroy(go);
        }
    }



    private void OnPartCreated(Clean_PartCreatedEvent @event)
    {
        //_views[@event.InstanceId] = @event.GameObject;

        //Register(@event.InstanceId, @event.GameObject);

        Debug.Log($"Added Part with ID  {@event.InstanceId}");

        _eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now });
    }


    public IEnumerable<GameObject> GetAllViews()
    {
        return _views.Values;
    }


    public bool TryGet(string partId, out DronePartView view)
    {

        return _partViews.TryGetValue(partId, out view) && view != null;
    }


    public bool TryGetAllChildrenIds(string partId, out List<string> childrenIds)
    {
        childrenIds = new();

        _views.TryGetValue(partId, out var go);
        if (go == null)
        {

            Debug.LogError($"Cannot find View with ID {partId}");
            return false;
        }

        var viewsWithChildren = go.GetComponentsInChildren<DronePartView>();

        foreach( var child in viewsWithChildren)
        {
            childrenIds.Add(child.InstanceId);
        }

        if(childrenIds.Count != 0 ) return true;
        return false;

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
        _partViews[instanceId] = view.GetComponent<DronePartView>();
    }

    internal IEnumerable<GameObject> GetAllGOs()
    {
        return _views.Values;
    }

    internal void Clear()
    {
        _views.Clear();
        _partViews.Clear();
    }

    public void Remove(string instanceId)
    {
        if (_views.TryGetValue(instanceId, out GameObject go))
        {
            _views.Remove(instanceId);
            _partViews.Remove(instanceId);
            if (go != null) GameObject.Destroy(go);
        }
    }
}
