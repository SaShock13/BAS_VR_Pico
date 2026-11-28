using System.Collections.Generic;
using UnityEngine;

// One presenter for the whole drone; driven by the existing flight LateUpdate.
public sealed class FlightMotorEffects
{
    private readonly List<Transform> _visuals = new();
    private readonly List<Transform> _hubs = new();
    private readonly DroneDebugInput _input;
    private readonly AudioSource _sound;

    public FlightMotorEffects(Transform drone, DroneDebugInput input, AudioClip clip,
        IReadOnlyList<DronePartView> propellers)
    {
        _input = input;
        _sound = drone.gameObject.AddComponent<AudioSource>();
        _sound.playOnAwake = false;
        _sound.loop = true;
        _sound.clip = clip;
        _sound.spatialBlend = 1f;
        _sound.panStereo = 0f;
        _sound.minDistance = 1.5f;
        _sound.maxDistance = 80f;
        _sound.rolloffMode = AudioRolloffMode.Logarithmic;
        _sound.dopplerLevel = 0f;
        _sound.volume = 0.65f;
        foreach (var propeller in propellers)
        {
            // Rotate visual branches only; socket roots and physical colliders stay fixed.
            foreach (var renderer in propeller.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // Some legacy meshes carry their colliders on the renderer itself.
                // Copy only rendering, keeping all original physics transforms stationary.
                var visual = new GameObject("Flight Rotor Visual").transform;
                visual.gameObject.layer = renderer.gameObject.layer;
                visual.SetParent(renderer.transform, false);
                visual.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = visual.gameObject.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                copy.shadowCastingMode = renderer.shadowCastingMode;
                copy.receiveShadows = renderer.receiveShadows;
                copy.renderingLayerMask = renderer.renderingLayerMask;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                copy.SetPropertyBlock(block);
                for (int slot = 0; slot < copy.sharedMaterials.Length; slot++)
                {
                    renderer.GetPropertyBlock(block, slot);
                    copy.SetPropertyBlock(block, slot);
                }
                renderer.enabled = false;
                _visuals.Add(visual);
                _hubs.Add(propeller.transform);
            }
        }
    }

    public void SetFirstPerson(bool firstPerson)
    {
        // FPV engine sound is centered in the head; external views locate it on the drone.
        if (_sound == null) return;
        _sound.panStereo = 0f;
        _sound.spatialBlend = firstPerson ? 0f : 1f;
    }

    public void Refresh()
    {
        if (_sound == null) return;
        bool running = _input.isActiveAndEnabled && _input.MotorsRunning && Time.timeScale > 0f;
        if (!running)
        {
            if (_sound.isPlaying) _sound.Stop();
            return;
        }
        if (!_sound.isPlaying && _sound.clip != null) _sound.Play();
        float throttle = Mathf.Clamp01(_input.CurrentInput.Throttle);
        _sound.pitch = Mathf.Lerp(0.8f, 1.5f, throttle);
        float angle = Mathf.Lerp(720f, 3600f, throttle) * Time.deltaTime;
        for (int i = 0; i < _visuals.Count; i++)
            if (_visuals[i] != null && _hubs[i] != null)
                _visuals[i].RotateAround(_hubs[i].position, _hubs[i].up, angle);
    }

    public void Stop()
    {
        if (_sound != null) _sound.Stop();
    }
}
