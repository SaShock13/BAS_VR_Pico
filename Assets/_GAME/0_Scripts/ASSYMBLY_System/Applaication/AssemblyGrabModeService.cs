using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class AssemblyGrabModeService : IInitializable, IDisposable
{
    private readonly IEventBus _events;
    private readonly Clean_AssemblySystem _assembly;
    private readonly PartViewRegistry _views;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
    private bool _detailMode;

    public AssemblyGrabModeService(IEventBus events, Clean_AssemblySystem assembly, PartViewRegistry views)
    {
        _events = events;
        _assembly = assembly;
        _views = views;
    }

    public void Initialize()
    {
        _subscriptions.Add(_events.Subscribe<SetAssemblyDetailModeRequest>(e => { _detailMode = e.IsHeld; Refresh(false); }));
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
        Refresh(false);
    }

    private PartDomainState FindRoot(PartDomainState part)
    {
        // The application rejects cycles; the cap also protects malformed legacy data.
        for (int depth = 0; depth < 256 && !string.IsNullOrEmpty(part.AttachedPartInstanceId); depth++)
        {
            if (!_assembly.TryGetPartDomainState(part.AttachedPartInstanceId, out var parent)) return null;
            part = parent;
        }
        return string.IsNullOrEmpty(part.AttachedPartInstanceId) ? part : null;
    }

    public void Refresh(bool updateBounds = true)
    {
        foreach (var go in _views.GetAllViews())
        {
            if (go == null || !go.TryGetComponent<DronePartView>(out var view) ||
                !_assembly.TryGetPartDomainState(view.InstanceId, out var part)) continue;
            var root = FindRoot(part);
            bool belongsToDrone = root != null && root.Type == DroneAssemblyRules.RootPartType;
            bool details = _detailMode;
            if (belongsToDrone && _views.TryGet(root.InstanceId, out var rootView) && rootView.IsGrabbed)
                details = false; // Keep an existing whole-drone grab stable until release.
            view.SetPartInteractionAllowed(!belongsToDrone || details || part == root);
            if (view.GrabEnvelope == null) continue;
            view.GrabEnvelope.SetDetailMode(details);
            if (!updateBounds || !DroneAssemblyRules.IsRoot(part)) continue;
            var bounds = new Bounds();
            bool found = false;
            foreach (var member in _views.GetAllViews())
            {
                if (member == null || !member.TryGetComponent<DronePartView>(out var child) ||
                    !_assembly.TryGetPartDomainState(child.InstanceId, out var childPart)) continue;
                if (FindRoot(childPart) == part) child.EncapsulateOwnGeometry(view.transform, ref bounds, ref found);
            }
            if (found) view.GrabEnvelope.SetBounds(bounds);
        }
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
    }
}
