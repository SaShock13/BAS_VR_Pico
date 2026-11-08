using System;
using UnityEngine;

[CreateAssetMenu(fileName = "VFXLibrary", menuName = "VFX/VFX Library")]
public sealed class VFXLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public VFXId Id;
        public GameObject Prefab;
        [Min(0.05f)] public float Lifetime;
    }

    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
    [SerializeField, Range(1, 32)] private int _maxConcurrentEffects = 8;

    public int MaxConcurrentEffects => Mathf.Clamp(_maxConcurrentEffects, 1, 32);

    public bool TryGet(VFXId id, out Entry entry)
    {
        foreach (var candidate in _entries)
        {
            if (candidate.Id != id || candidate.Prefab == null) continue;
            entry = candidate;
            return true;
        }
        entry = default;
        return false;
    }
}
