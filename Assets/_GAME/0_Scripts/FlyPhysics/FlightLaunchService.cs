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
    public string PanelId;
}

public sealed class FlightLaunchService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly INotificationService _notifications;
    private readonly DroneValidationStateService _validation;
    private readonly FlightEnvironmentCatalog _catalog;
    private readonly bool _launchStoredDrones;
    private readonly IGarageService _garage;
    private IDisposable _missionSubscription;
    private IDisposable _modeSubscription, _launchSubscription;
    private bool _launching;
    private FlightLaunchApprovedEvent _pendingLaunch;

    public FlightLaunchService(IEventBus bus, ISelectionService selection,
        Clean_AssemblySystem assembly, INotificationService notifications,
        [InjectOptional] DroneValidationStateService validation = null,
        [InjectOptional] FlightEnvironmentCatalog catalog = null,
        [InjectOptional] bool launchStoredDrones = false,
        [InjectOptional] IGarageService garage = null)
    {
        _bus = bus;
        _selection = selection;
        _assembly = assembly;
        _notifications = notifications;
        _validation = validation;
        _catalog = catalog;
        _launchStoredDrones = launchStoredDrones;
        _garage = garage;
    }

    public void Initialize()
    {
        _modeSubscription = _bus.Subscribe<FlightModeRequest>(e => FlightSession.Mode = e.Mode);
        _launchSubscription = _bus.Subscribe<LaunchFlightRequest>(e => Launch(panelId: e.PanelId));
        _missionSubscription = _bus.Subscribe<ConfirmFlightMissionRequest>(e => Launch(e.MissionId));
    }

    private void Launch(string missionId = null, string panelId = null)
    {
        if (_launching) return;
        var selected = _selection.Current;
        if (!selected.HasValue || !_assembly.TryGetDroneForPart(selected.Value.PartId, out var drone))
        {
            _notifications.Warning("Выберите дрон для полёта");
            return;
        }
        var stored = _launchStoredDrones ? _garage?.GetByDroneId(drone.InstanceId)?.Assembly : null;
        if (_launchStoredDrones && (stored?.Parts == null || stored.Parts.Count == 0))
        {
            _notifications.Warning("Выберите сохранённый дрон из гаража");
            return;
        }
        if (_catalog == null || _catalog.Missions.Count == 0)
        {
            _notifications.Warning("Каталог полётных миссий не настроен");
            return;
        }
        if (_validation == null)
        {
            _notifications.Error("Проверка дрона недоступна.");
            return;
        }
        if (missionId == null)
        {
            if (!_validation.ValidateForFlight(drone.InstanceId)) return;
            _bus.Publish(new OpenFlightMissionSelection { PanelId = panelId });
            return;
        }
        var entry = _catalog.FindMission(missionId);
        if (entry != null && !_validation.MeetsMissionRequirements(drone.InstanceId, entry.mission.Requirements))
            return;
        var environment = entry == null ? null : _catalog.Find(entry.environmentId);
        if (environment == null || !UnityEngine.Application.CanStreamedLevelBeLoaded(environment.scenePath)
            || !UnityEngine.Application.CanStreamedLevelBeLoaded(_catalog.coreScenePath))
        {
            _notifications.Warning("Сцена локации или общая сцена полёта отсутствует в сборке");
            return;
        }
        FlightSession.EnvironmentId = environment.id;
        FlightSession.Mission = entry.mission;
        FlightSession.Drone = stored ?? _assembly.BuildSaveDataForDrone(drone.InstanceId);
        FlightSession.Physics = null;
        if (!_launchStoredDrones) _assembly.Save();
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
        _missionSubscription?.Dispose();
    }
}
