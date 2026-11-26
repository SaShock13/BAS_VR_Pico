using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class RenderDistancePolicy
{
    public const float Default = 120f;
    public const float Minimum = 60f;
    public const float Maximum = 300f;
    public const float Step = 20f;
    public const string PreferenceKey = "Graphics.RenderDistance";
    public const string FogPreferenceKey = "Graphics.FogEnabled";

    public static float Clamp(float distance) => float.IsNaN(distance) || float.IsInfinity(distance)
        ? Default : Mathf.Clamp(distance, Minimum, Maximum);
    public static float FogStart(float distance) => Clamp(distance) / 3f;
    public static float FogEnd(float distance) => Clamp(distance) * (5f / 6f);
}

public sealed class SetRenderDistanceRequest : IAppEvent
{
    public string EventId => nameof(SetRenderDistanceRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public float Distance;
}

public sealed class SetFogEnabledRequest : IAppEvent
{
    public string EventId => nameof(SetFogEnabledRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Enabled;
}

public interface IRenderDistanceSettings
{
    float Distance { get; }
    bool FogEnabled { get; }
    event Action<float> Changed;
    void RegisterCamera(Camera camera);
    void UnregisterCamera(Camera camera);
}

// Scene-scoped service. Work happens only on setting changes and scene activation.
public sealed class RenderDistanceSettings : IRenderDistanceSettings, IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly Scene _owner;
    private readonly List<Camera> _cameras = new List<Camera>(4);
    private IDisposable _requestSubscription;
    private IDisposable _fogSubscription;
    public float Distance { get; private set; }
    public bool FogEnabled { get; private set; }
    public event Action<float> Changed;

    public RenderDistanceSettings(IEventBus bus, Scene owner)
    {
        _bus = bus;
        _owner = owner;
        Distance = RenderDistancePolicy.Clamp(PlayerPrefs.GetFloat(
            RenderDistancePolicy.PreferenceKey, RenderDistancePolicy.Default));
        FogEnabled = PlayerPrefs.GetInt(RenderDistancePolicy.FogPreferenceKey, 1) != 0;
    }

    public void Initialize()
    {
        _requestSubscription = _bus.Subscribe<SetRenderDistanceRequest>(OnRequest);
        _fogSubscription = _bus.Subscribe<SetFogEnabledRequest>(OnFogRequest);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        // Include inactive scene cameras; UI preview render textures keep their own clipping.
        foreach (var root in _owner.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                if (camera.cameraType == CameraType.Game && camera.targetTexture == null)
                    RegisterCamera(camera);
        ApplyFog();
    }

    public void RegisterCamera(Camera camera)
    {
        if (camera == null) return;
        if (!_cameras.Contains(camera)) _cameras.Add(camera);
        camera.farClipPlane = Distance;
    }

    public void UnregisterCamera(Camera camera) => _cameras.Remove(camera);

    private void OnRequest(SetRenderDistanceRequest request)
    {
        Distance = RenderDistancePolicy.Clamp(request.Distance);
        for (int i = _cameras.Count - 1; i >= 0; i--)
        {
            if (_cameras[i] == null) _cameras.RemoveAt(i);
            else _cameras[i].farClipPlane = Distance;
        }
        ApplyFog();
        PlayerPrefs.SetFloat(RenderDistancePolicy.PreferenceKey, Distance);
        PlayerPrefs.Save();
        Changed?.Invoke(Distance);
    }

    private void OnActiveSceneChanged(Scene previous, Scene current) => ApplyFog();

    private void OnFogRequest(SetFogEnabledRequest request)
    {
        FogEnabled = request.Enabled;
        ApplyFog();
        PlayerPrefs.SetInt(RenderDistancePolicy.FogPreferenceKey, FogEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke(Distance);
    }

    private void ApplyFog()
    {
        // BootMenu stores the preference without introducing fog over its menu background.
        if (_owner.name != "ConstructorScene" && _owner.name != "FlightCoreScene") return;
        if (!_owner.isLoaded) return;
        RenderSettings.fog = FogEnabled;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = RenderDistancePolicy.FogStart(Distance);
        RenderSettings.fogEndDistance = RenderDistancePolicy.FogEnd(Distance);
    }

    public void Dispose()
    {
        _requestSubscription?.Dispose();
        _fogSubscription?.Dispose();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        _cameras.Clear();
        Changed = null;
    }
}
