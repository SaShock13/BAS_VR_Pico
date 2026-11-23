using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Zenject;

public sealed class AssemblyGrabModeService : IInitializable, IDisposable
{
    private sealed class Routing
    {
        public DronePartView View;
        public XRGrabInteractable Grab;
        public Collider[] Own;
        public readonly List<Collider> Desired = new List<Collider>();
        public Routing Target, NextTarget;
        public XRInteractionManager Manager;
        public bool Registered, Changed;
    }

    private readonly IEventBus _events;
    private readonly Clean_AssemblySystem _assembly;
    private readonly PartViewRegistry _views;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
    private readonly Dictionary<DronePartView, Routing> _routing = new Dictionary<DronePartView, Routing>();
    private readonly List<DronePartView> _removed = new List<DronePartView>();
    private bool _detailMode, _refreshing;
    public event Action RoutesChanged;

    public AssemblyGrabModeService(IEventBus events, Clean_AssemblySystem assembly, PartViewRegistry views)
    {
        _events = events;
        _assembly = assembly;
        _views = views;
    }

    public void Initialize()
    {
        _subscriptions.Add(_events.Subscribe<SetAssemblyDetailModeRequest>(e => { _detailMode = e.IsHeld; Refresh(); }));
        _subscriptions.Add(_events.Subscribe<DronesRebuiltEvent>(e => Refresh()));
        _subscriptions.Add(_events.Subscribe<Clean_PartCreatedEvent>(e => Refresh()));
        _subscriptions.Add(_events.Subscribe<AssemblyGeometryChangedEvent>(e => Refresh()));
        _subscriptions.Add(_events.Subscribe<PartTransformChangedEvent>(e => Refresh()));
        _subscriptions.Add(_events.Subscribe<PartGrabStartedEvent>(OnGrabStarted));
        Refresh();
    }

    private void OnGrabStarted(PartGrabStartedEvent request)
    {
        if (!_assembly.TryGetPartDomainState(request.InstanceId, out var part)) return;
        if (part.LifecycleState == PartLifecycleState.Installed &&
            (_detailMode || FindRoot(part)?.Type != DroneAssemblyRules.RootPartType))
            _events.Publish(new PartSocketDetachRequest { PartInstanceId = part.InstanceId, Timestamp = DateTime.UtcNow });
        Refresh();
    }

    private PartDomainState FindRoot(PartDomainState part)
    {
        for (int depth = 0; depth < 256 && !string.IsNullOrEmpty(part.AttachedPartInstanceId); depth++)
        {
            if (!_assembly.TryGetPartDomainState(part.AttachedPartInstanceId, out var parent)) return null;
            part = parent;
        }
        return string.IsNullOrEmpty(part.AttachedPartInstanceId) ? part : null;
    }

    // Kept for existing callers; assembly-sized bounds are no longer calculated.
    public void Refresh(bool updateBounds = true)
    {
        if (_refreshing) return;
        _refreshing = true;
        try { RefreshRouting(); }
        finally { _refreshing = false; }
    }

    private void RefreshRouting()
    {
        _removed.Clear();
        foreach (var pair in _routing)
            if (pair.Key == null || !_views.TryGet(pair.Key.InstanceId, out var live) || live != pair.Key)
                _removed.Add(pair.Key);
        foreach (var go in _views.GetAllViews())
        {
            if (go == null || !go.TryGetComponent<DronePartView>(out var view) ||
                view.GrabInteractable == null || _routing.ContainsKey(view)) continue;
            var grab = view.GrabInteractable;
            var route = new Routing { View = view, Grab = grab, Own = grab.colliders.ToArray() };
            route.Target = route;
            _routing.Add(view, route);
            // A legacy broad collider becomes local to its own part and stays enabled in both modes.
            if (view.GrabEnvelope != null && view.HasMountingBounds)
            {
                view.GrabEnvelope.SetBounds(view.LocalMountingBounds);
                view.GrabEnvelope.SetDetailMode(false);
            }
        }

        foreach (var route in _routing.Values)
        {
            route.Desired.Clear();
            route.NextTarget = route;
            if (route.View == null || _removed.Contains(route.View) ||
                !_assembly.TryGetPartDomainState(route.View.InstanceId, out var part)) continue;
            var root = FindRoot(part);
            if (root == null || root.Type != DroneAssemblyRules.RootPartType ||
                !_views.TryGet(root.InstanceId, out var rootView) || !_routing.TryGetValue(rootView, out var rootRoute)) continue;
            if (!_detailMode || rootView.IsGrabbed) route.NextTarget = rootRoute;
        }
        foreach (var route in _routing.Values)
            if (route.Grab != null)
                foreach (var collider in route.Own)
                    if (collider != null) route.NextTarget.Desired.Add(collider);

        // Unregister cancels selection. Defer transitions affecting held objects until release.
        foreach (var route in _routing.Values)
        {
            route.Changed = route.Grab != null && !SameColliders(route.Grab.colliders, route.Desired);
            if (route.Changed && route.View.IsGrabbed) return;
        }
        foreach (var route in _routing.Values)
        {
            if (!route.Changed) continue;
            route.Manager = route.Grab.interactionManager;
            route.Registered = route.Manager != null && route.Manager.IsRegistered((IXRInteractable)route.Grab);
            if (route.Registered) route.Manager.UnregisterInteractable((IXRInteractable)route.Grab);
        }
        // Unregister ALL changed owners before assigning any collider to a different owner.
        foreach (var route in _routing.Values)
        {
            if (!route.Changed) continue;
            route.Grab.colliders.Clear();
            route.Grab.colliders.AddRange(route.Desired);
        }
        bool changed = false;
        foreach (var route in _routing.Values)
        {
            if (route.Grab == null) continue;
            changed |= route.Target != route.NextTarget || route.Changed;
            route.Target = route.NextTarget;
            route.View.SetPartInteractionAllowed(route.Target == route);
            if (route.Changed && route.Registered && route.Grab.isActiveAndEnabled)
                route.Manager.RegisterInteractable((IXRInteractable)route.Grab);
        }
        foreach (var removed in _removed) _routing.Remove(removed);
        if (changed || _removed.Count > 0) RoutesChanged?.Invoke();
    }

    private static bool SameColliders(List<Collider> actual, List<Collider> desired)
    {
        if (actual.Count != desired.Count) return false;
        for (int i = 0; i < actual.Count; i++)
            if (actual[i] != desired[i]) return false;
        return true;
    }

    public void GetHighlightTargets(DronePartView view, List<DronePartView> results)
    {
        results.Clear();
        if (view == null) return;
        if (!_routing.TryGetValue(view, out var hovered)) { results.Add(view); return; }
        foreach (var route in _routing.Values)
            if (route.View != null && route.Target == hovered.Target)
                results.Add(route.View);
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
        foreach (var route in _routing.Values)
            if (route.Grab != null && route.Grab.interactionManager != null)
                route.Grab.interactionManager.UnregisterInteractable((IXRInteractable)route.Grab);
        foreach (var route in _routing.Values)
        {
            if (route.Grab == null) continue;
            route.Grab.colliders.Clear();
            foreach (var collider in route.Own)
                if (collider != null) route.Grab.colliders.Add(collider);
            route.View.SetPartInteractionAllowed(true);
        }
        foreach (var route in _routing.Values)
            if (route.Grab != null && route.Grab.isActiveAndEnabled && route.Grab.interactionManager != null)
                route.Grab.interactionManager.RegisterInteractable((IXRInteractable)route.Grab);
        _routing.Clear();
        RoutesChanged?.Invoke();
    }
}
