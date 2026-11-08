using System;
using TMPro;
using UnityEngine;
using Zenject;

public sealed class FlightMenuView : MonoBehaviour
{
    [SerializeField] private TMP_Text pauseLabel;
    private IEventBus _bus;
    private IDisposable _pauseSubscription;

    [Inject]
    public void Construct(IEventBus bus)
    {
        _bus = bus;
        _pauseSubscription = bus.Subscribe<PauseStateChangedEvent>(e =>
            pauseLabel.text = e.IsPaused ? "Продолжить" : "Пауза");
    }

    public void TogglePause() => _bus.Publish(new ToggleFlightPauseRequest());
    public void ReturnToAssembly() => _bus.Publish(new ReturnToAssemblyRequest());

    private void OnDestroy() => _pauseSubscription?.Dispose();
}
