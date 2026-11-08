using System;
using System.Collections.Generic;
using UnityEngine;

// Short, bounded bursts: no Update, coroutine or permanent scene object.
public sealed class VFXPlayer : IVFXPlayer, IDisposable
{
    private readonly VFXLibrary _library;
    private readonly List<GameObject> _active = new List<GameObject>(32);
    private bool _disposed;

    public VFXPlayer(VFXLibrary library) { _library = library; }

    public void Play(VFXId id, Vector3 position, Vector3 scale)
    {
        if (_disposed || _library == null || !_library.TryGet(id, out var entry)) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i] == null) _active.RemoveAt(i);
        if (_active.Count >= _library.MaxConcurrentEffects) return;

        var instance = UnityEngine.Object.Instantiate(entry.Prefab, position, Quaternion.identity);
        instance.transform.localScale = scale;
        instance.SetActive(true);
        _active.Add(instance);
        UnityEngine.Object.Destroy(instance, entry.Lifetime > 0f ? entry.Lifetime : 1f);
    }

    public void Dispose()
    {
        _disposed = true;
        foreach (var instance in _active)
            if (instance != null) UnityEngine.Object.Destroy(instance);
        _active.Clear();
    }
}
