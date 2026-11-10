using System;
using UnityEngine;
using Zenject;

// One bounded pool per container. No per-frame managed allocations.
public sealed class SFXPlayer : ISFXPlayer, IInitializable, ITickable, IDisposable
{
    private struct Voice
    {
        public AudioSource Source;
        public SFXHandle Handle;
        public SFXId Id;
        public Transform Target;
        public bool Following;
        public float BaseVolume;
    }

    private static long _nextHandle;
    private readonly SFXLibrary _library;
    private Voice[] _voices;
    private GameObject _root;
    private int _activeCount;
    private bool _disposed;

    public SFXPlayer(SFXLibrary library) { _library = library; }

    public void Initialize()
    {
        if (_disposed || _voices != null || _library == null) return;
        _root = new GameObject("SFX Audio Pool");
        _voices = new Voice[_library.MaxVoices];
        for (int i = 0; i < _voices.Length; i++)
        {
            var child = new GameObject("SFX Voice " + i);
            child.transform.SetParent(_root.transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            child.SetActive(false);
            _voices[i].Source = source;
        }
    }

    public SFXHandle Play(SFXId id, Vector3 position = default, float volume = 1f, Transform followTarget = null)
    {
        if (_disposed || _library == null || !_library.TryGet(id, out var entry)) return default;
        Initialize();
        ReclaimFinished();
        int free = -1, count = 0;
        for (int i = 0; i < _voices.Length; i++)
        {
            if (!_voices[i].Handle.IsValid) { if (free < 0 && _voices[i].Source != null) free = i; }
            else if (_voices[i].Id == id) count++;
        }
        if (free < 0 || count >= Mathf.Clamp(entry.MaxConcurrent, 1, 32)) return default;
        var clips = entry.Clips;
        if (clips == null || clips.Length == 0) return default;
        // Select uniformly among assigned variants, tolerating empty slots.
        int valid = 0;
        AudioClip clip = null;
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] != null && UnityEngine.Random.Range(0, ++valid) == 0) clip = clips[i];
        if (clip == null) return default;

        ref var voice = ref _voices[free];
        var source = voice.Source;
        source.transform.position = followTarget != null ? followTarget.position : position;
        source.clip = clip;
        source.outputAudioMixerGroup = entry.Output;
        voice.BaseVolume = Mathf.Clamp01(entry.Volume);
        source.volume = voice.BaseVolume * Mathf.Clamp01(volume);
        float minPitch = Mathf.Clamp(Mathf.Min(entry.PitchRange.x, entry.PitchRange.y), 0.1f, 3f);
        float maxPitch = Mathf.Clamp(Mathf.Max(entry.PitchRange.x, entry.PitchRange.y), minPitch, 3f);
        source.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        source.spatialBlend = Mathf.Clamp01(entry.SpatialBlend);
        source.minDistance = Mathf.Max(0.01f, entry.MinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, entry.MaxDistance);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.priority = Mathf.Clamp(entry.Priority, 0, 256);
        source.loop = entry.Loop;
        voice.Id = id;
        voice.Target = followTarget;
        voice.Following = followTarget != null;
        voice.Handle = new SFXHandle(++_nextHandle);
        _activeCount++;
        source.gameObject.SetActive(true);
        source.Play();
        return voice.Handle;
    }

    public void Tick()
    {
        if (_disposed || _activeCount == 0) return;
        ReclaimFinished();
        for (int i = 0; i < _voices.Length; i++)
        {
            ref var voice = ref _voices[i];
            if (!voice.Handle.IsValid || !voice.Following) continue;
            if (voice.Source == null || voice.Target == null) Release(i);
            else voice.Source.transform.position = voice.Target.position;
        }
    }

    private void ReclaimFinished()
    {
        if (_voices == null || _activeCount == 0 || AudioListener.pause) return;
        for (int i = 0; i < _voices.Length; i++)
            if (_voices[i].Handle.IsValid && (_voices[i].Source == null || !_voices[i].Source.isPlaying))
                Release(i);
    }

    private void Release(int index)
    {
        ref var voice = ref _voices[index];
        var source = voice.Source;
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.gameObject.SetActive(false);
        }
        voice.Target = null;
        voice.Following = false;
        voice.Handle = default;
        _activeCount--;
    }

    public bool Stop(SFXHandle handle)
    {
        if (_disposed || _voices == null || !handle.IsValid) return false;
        for (int i = 0; i < _voices.Length; i++)
            if (_voices[i].Handle.Equals(handle)) { Release(i); return true; }
        return false;
    }

    public void StopAll(SFXId id)
    {
        if (_disposed || _voices == null) return;
        for (int i = 0; i < _voices.Length; i++)
            if (_voices[i].Handle.IsValid && _voices[i].Id == id) Release(i);
    }

    public bool SetVolume(SFXHandle handle, float volume)
    {
        if (_disposed || _voices == null || !handle.IsValid) return false;
        for (int i = 0; i < _voices.Length; i++)
            if (_voices[i].Handle.Equals(handle) && _voices[i].Source != null)
            {
                _voices[i].Source.volume = _voices[i].BaseVolume * Mathf.Clamp01(volume);
                return true;
            }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_voices != null)
            for (int i = 0; i < _voices.Length; i++)
                if (_voices[i].Handle.IsValid) Release(i);
        if (_root != null) UnityEngine.Object.Destroy(_root);
        _voices = null;
    }
}

