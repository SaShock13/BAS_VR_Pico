using UnityEngine;

public interface ISFXPlayer
{
    SFXHandle Play(SFXId id, Vector3 position = default, float volume = 1f, Transform followTarget = null);
    bool Stop(SFXHandle handle);
    void StopAll(SFXId id);
    bool SetVolume(SFXHandle handle, float volume);
}

