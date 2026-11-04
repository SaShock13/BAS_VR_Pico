using System;
using UnityEngine.SceneManagement;
using Zenject;

public class FlightModeRequest : IAppEvent
{
    public string EventId => nameof(FlightModeRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public FlightMode Mode;
}

public class LaunchFlightRequest : IAppEvent
{
    public string EventId => nameof(LaunchFlightRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class FlightLaunchService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly INotificationService _notifications;
    private readonly DroneValidationStateService _validation;
    private IDisposable _modeSubscription, _launchSubscription;
    private bool _launching;

    public FlightLaunchService(IEventBus bus, ISelectionService selection,
        Clean_AssemblySystem assembly, INotificationService notifications,
        [InjectOptional] DroneValidationStateService validation = null)
    {
        _bus = bus;
        _selection = selection;
        _assembly = assembly;
        _notifications = notifications;
        _validation = validation;
    }

    public void Initialize()
    {
        _modeSubscription = _bus.Subscribe<FlightModeRequest>(e => FlightSession.Mode = e.Mode);
        _launchSubscription = _bus.Subscribe<LaunchFlightRequest>(_ => Launch());
    }

    private void Launch()
    {
        if (_launching) return;
        var selected = _selection.Current;
        if (!selected.HasValue || !_assembly.TryGetDroneForPart(selected.Value.PartId, out var drone))
        {
            _notifications.Warning("Выберите дрон для полёта");
            return;
        }
        if (_validation == null || !_validation.IsValidated(drone.InstanceId))
        {
            _notifications.Warning("Валидация не пройдена. Нажмите «Пройти валидацию» в панели дрона.", 5f);
            return;
        }
        FlightSession.Drone = _assembly.BuildSaveDataForDrone(drone.InstanceId);
        FlightSession.Physics = null;
        _assembly.Save();
        _launching = true;
        SceneManager.LoadScene("FlightScene");
    }

    public void Dispose()
    {
        _modeSubscription?.Dispose();
        _launchSubscription?.Dispose();
    }
}
