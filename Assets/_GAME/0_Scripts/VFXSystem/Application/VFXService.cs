using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class VFXService : IVFXService, IInitializable, IDisposable
{
    private readonly IVFXPlayer _player;
    private readonly IEventBus _eventBus;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

    public VFXService(IVFXPlayer player, IEventBus eventBus)
    {
        _player = player;
        _eventBus = eventBus;
    }

    public void Initialize()
    {
        if (_subscriptions.Count != 0) return;
        _subscriptions.Add(_eventBus.Subscribe<PlayVFXRequest>(e => PlayVFX(e.VFXId, e.Position, e.Scale)));
        _subscriptions.Add(_eventBus.Subscribe<PartSocketAttachedEvent>(e => PlayVFX(VFXId.PartAttached, e.Position, Vector3.one)));
        _subscriptions.Add(_eventBus.Subscribe<PartFreeAttachedEvent>(e => PlayVFX(VFXId.PartAttached, e.Position, Vector3.one)));
        _subscriptions.Add(_eventBus.Subscribe<PartSocketDetachedEvent>(e => PlayVFX(VFXId.PartDetached, e.Position, Vector3.one)));
        _subscriptions.Add(_eventBus.Subscribe<Clean_PartDeletedEvent>(e => PlayVFX(VFXId.PartDeleted, e.Position, e.Scale)));
        _subscriptions.Add(_eventBus.Subscribe<PartDublicatedEvent>(e => PlayVFX(VFXId.PartDublicated, e.Position, Vector3.one)));
    }

    public void PlayVFX(VFXId id, Vector3 position, Vector3 scale) => _player.Play(id, position, scale);

    public void Dispose()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
    }
}
