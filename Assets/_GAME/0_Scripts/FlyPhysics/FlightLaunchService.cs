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
    private readonly FlightEnvironmentCatalog _catalog;
    private IDisposable _environmentSubscription;
    private IDisposable _modeSubscription, _launchSubscription;
    private bool _launching;
    private FlightLaunchApprovedEvent _pendingLaunch;

    public FlightLaunchService(IEventBus bus, ISelectionService selection,
        Clean_AssemblySystem assembly, INotificationService notifications,
        [InjectOptional] DroneValidationStateService validation = null,
        [InjectOptional] FlightEnvironmentCatalog catalog = null)
    {
        _bus = bus;
        _selection = selection;
        _assembly = assembly;
        _notifications = notifications;
        _validation = validation;
        _catalog = catalog;
    }

    public void Initialize()
    {
        _modeSubscription = _bus.Subscribe<FlightModeRequest>(e => FlightSession.Mode = e.Mode);
        _launchSubscription = _bus.Subscribe<LaunchFlightRequest>(_ => Launch());
        _environmentSubscription = _bus.Subscribe<ConfirmFlightEnvironmentRequest>(e => Launch(e.EnvironmentId));
    }

    private void Launch(string environmentId = null)
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
        if (_catalog == null || _catalog.Environments.Count == 0)
        {
            _notifications.Warning("Каталог полётных локаций не настроен");
            return;
        }
        if (environmentId == null)
        {
            _bus.Publish(new OpenFlightEnvironmentSelection());
            return;
        }
        var environment = _catalog.Find(environmentId);
        if (environment == null || !UnityEngine.Application.CanStreamedLevelBeLoaded(environment.scenePath)
            || !UnityEngine.Application.CanStreamedLevelBeLoaded(_catalog.coreScenePath))
        {
            _notifications.Warning("Сцена локации или общая сцена полёта отсутствует в сборке");
            return;
        }
        FlightSession.EnvironmentId = environmentId;
        FlightSession.Drone = _assembly.BuildSaveDataForDrone(drone.InstanceId);
        FlightSession.Physics = null;
        _assembly.Save();
        _launching = true;
        var approved = new FlightLaunchApprovedEvent(
            LoadCore,
            () => _launching = false);
        _pendingLaunch = approved;
        _bus.Publish(approved);
        if (!approved.IsDeferred)
            LoadCore();
    }

    private void LoadCore()
    {
        try { SceneManager.LoadSceneAsync(_catalog.coreScenePath, LoadSceneMode.Single); }
        catch (Exception exception)
        {
            _launching = false;
            _notifications.Warning("Не удалось загрузить полётную сцену");
            UnityEngine.Debug.LogException(exception);
        }
    }

    public void Dispose()
    {
        _pendingLaunch?.Cancel();
        _modeSubscription?.Dispose();
        _launchSubscription?.Dispose();
        _environmentSubscription?.Dispose();
    }
}
