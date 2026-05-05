using System.Collections.Generic;
using UnityEngine;

public class SocketRegistry : ISocketResolver
{
    private readonly Dictionary<string, Transform> _sockets = new();

    public void Register(string id, Transform transform)
    {
        if (_sockets.ContainsKey(id))
        {
            Debug.LogError($"Socket already registered: {id}");
            return;
        }

        _sockets.Add(id, transform);
    }

    public Transform Resolve(string socketId)
    {
        if (_sockets.TryGetValue(socketId, out var socket))
            return socket;

        Debug.LogError($"Socket not found: {socketId}");
        return null;
    }

    public void Clear()
    {
        _sockets.Clear();
    }
}