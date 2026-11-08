using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public sealed class ToggleFlightPauseRequest : IAppEvent
{
    public string EventId => nameof(ToggleFlightPauseRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ReturnToAssemblyRequest : IAppEvent
{
    public string EventId => nameof(ReturnToAssemblyRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class FlightMenuService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private IDisposable _pauseSubscription, _returnSubscription;
    private bool _paused, _returning;
    private float _previousTimeScale;
    private bool _previousAudioPause;

    public FlightMenuService(IEventBus bus) => _bus = bus;

    public void Initialize()
    {
        _pauseSubscription = _bus.Subscribe<ToggleFlightPauseRequest>(_ => TogglePause());
        _returnSubscription = _bus.Subscribe<ReturnToAssemblyRequest>(_ => Return());
    }

    private void TogglePause()
    {
        if (_returning) return;
        if (_paused) Resume();
        else
        {
            _previousTimeScale = Time.timeScale;
            _previousAudioPause = AudioListener.pause;
            _paused = true;
            Time.timeScale = 0;
            AudioListener.pause = true;
        }
        _bus.Publish(new PauseStateChangedEvent
        {
            IsPaused = _paused, PausedState = AppState.Flight,
            Reason = PauseInitiator.User, Timestamp = DateTime.UtcNow
        });
    }

    private void Resume()
    {
        if (!_paused) return;
        Time.timeScale = _previousTimeScale;
        AudioListener.pause = _previousAudioPause;
        _paused = false;
    }

    private void Return()
    {
        if (_returning) return;
        _returning = true;
        Resume();
        try { SceneManager.LoadScene(MenuNavigationService.ConstructorSceneName); }
        catch (Exception exception)
        {
            _returning = false;
            Debug.LogException(exception);
        }
    }

    public void Dispose()
    {
        _pauseSubscription?.Dispose();
        _returnSubscription?.Dispose();
        Resume();
    }
}
