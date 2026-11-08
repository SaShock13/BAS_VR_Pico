using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class SFXService : ISFXService, IInitializable, IDisposable
{
    private readonly ISFXPlayer _player;
    private readonly IEventBus _eventBus;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>(7);
    private bool _disposed;

    public SFXService(ISFXPlayer player, IEventBus eventBus)
    {
        _player = player;
        _eventBus = eventBus;
    }

    public void Initialize()
    {
        if (_disposed || _subscriptions.Count != 0) return;
        _subscriptions.Add(_eventBus.Subscribe<PlaySFXRequest>(e => Play(e.SFXId, e.Position, e.Volume, e.FollowTarget)));
        _subscriptions.Add(_eventBus.Subscribe<StopSFXRequest>(e =>
        {
            if (e.Handle.IsValid) Stop(e.Handle);
            else StopAll(e.SFXId);
        }));
        _subscriptions.Add(_eventBus.Subscribe<PartSocketAttachedEvent>(e => Play(SFXId.PartAttached, e.Position)));
        _subscriptions.Add(_eventBus.Subscribe<PartFreeAttachedEvent>(e => Play(SFXId.PartAttached, e.Position)));
        _subscriptions.Add(_eventBus.Subscribe<PartSocketDetachedEvent>(e => Play(SFXId.PartDetached, e.Position)));
        _subscriptions.Add(_eventBus.Subscribe<Clean_PartDeletedEvent>(e => Play(SFXId.PartDeleted, e.Position)));
        _subscriptions.Add(_eventBus.Subscribe<PartDublicatedEvent>(e => Play(SFXId.PartDublicated, e.Position)));
    }

    public SFXHandle Play(SFXId id, Vector3 position = default, float volume = 1f, Transform followTarget = null)
        => _disposed ? default : _player.Play(id, position, volume, followTarget);
    public bool Stop(SFXHandle handle) => !_disposed && _player.Stop(handle);
    public void StopAll(SFXId id) { if (!_disposed) _player.StopAll(id); }
    public bool SetVolume(SFXHandle handle, float volume) => !_disposed && _player.SetVolume(handle, volume);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
    }
}

