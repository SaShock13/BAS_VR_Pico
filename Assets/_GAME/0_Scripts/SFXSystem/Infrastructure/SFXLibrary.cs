using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SFXLibrary", menuName = "SFX/SFX Library")]
public sealed class SFXLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public SFXId Id;
        public AudioClip[] Clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float Volume = 1f;
        public Vector2 PitchRange = Vector2.one;
        [Range(0f, 1f)] public float SpatialBlend = 1f;
        [Min(0.01f)] public float MinDistance = 1f;
        [Min(0.01f)] public float MaxDistance = 20f;
        public bool Loop;
        [Range(0, 256)] public int Priority = 128;
        [Range(1, 32)] public int MaxConcurrent = 4;
        public AudioMixerGroup Output;
    }

    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
    [SerializeField, Range(1, 32)] private int _maxVoices = 16;
    public int MaxVoices => Mathf.Clamp(_maxVoices, 1, 32);

    public bool TryGet(SFXId id, out Entry entry)
    {
        if (_entries != null)
            foreach (var candidate in _entries)
                if (candidate != null && candidate.Id == id)
                {
                    entry = candidate;
                    return true;
                }
        entry = null;
        return false;
    }
}

