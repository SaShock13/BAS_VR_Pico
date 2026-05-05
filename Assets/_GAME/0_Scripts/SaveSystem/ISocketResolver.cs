using UnityEngine;

public interface ISocketResolver
{
    Transform Resolve(string socketId);
}