using System;
using UnityEngine;

[Serializable]
public class TransformSaveData
{
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    // Older saves have no scale fields and keep the prefab's original scale.
    public bool HasLocalScale;
    public Vector3 LocalScale;
}
