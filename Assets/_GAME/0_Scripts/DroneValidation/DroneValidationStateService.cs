using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

public sealed class ValidateDroneRequest : IAppEvent
{
    public string EventId => nameof(ValidateDroneRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DroneId;
}

/// <summary>Validation belongs to the current assembly session and is never restored from saves.</summary>
public sealed class DroneValidationStateService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IPartConfigRepository _configs;
    private readonly DroneReadinessService _readiness;
    private readonly INotificationService _notifications;
    private readonly Dictionary<string, DroneReadinessResult> _results = new();
    private readonly List<IDisposable> _subscriptions = new();
    public event Action Changed;

    public DroneValidationStateService(IEventBus bus, Clean_AssemblySystem assembly,
        IPartConfigRepository configs, DroneReadinessService readiness, INotificationService notifications)
    {
        _bus = bus;
        _assembly = assembly;
        _configs = configs;
        _readiness = readiness;
        _notifications = notifications;
    }

    public void Initialize()
    {
        _subscriptions.Add(_bus.Subscribe<ValidateDroneRequest>(OnValidateRequested));
        _subscriptions.Add(_bus.Subscribe<DronesRebuiltEvent>(_ => Invalidate()));
        _subscriptions.Add(_bus.Subscribe<AssemblyGeometryChangedEvent>(_ => Invalidate()));
        _subscriptions.Add(_bus.Subscribe<PartVisualChangedEvent>(_ => Invalidate()));
    }

    private void OnValidateRequested(ValidateDroneRequest request)
    {
        var result = Validate(request.DroneId);
        if (result == null)
        {
            _notifications.Warning("Не удалось проверить дрон. Выберите загруженный дрон и повторите валидацию.");
            return;
        }
        ShowValidationResult(result);
    }

    private void ShowValidationResult(DroneReadinessResult result)
    {
        var details = result.GetDetailsText();
        if (details.Length == 0)
        {
            if (result.IsReady) _notifications.Info(result.StatusText);
            else _notifications.Warning(result.StatusText);
            return;
        }

        // Keep every reason readable in the fixed-size VR toast instead of truncating it.
        const int pageLength = 140;
        while (details.Length > 0)
        {
            int length = Math.Min(pageLength, details.Length);
            if (length < details.Length)
            {
                int boundary = details.LastIndexOf(' ', length - 1, length);
                if (boundary > 0) length = boundary;
            }
            int lineBreaks = 0;
            for (int i = 0; i < length; i++)
                if (details[i] == '\n' && ++lineBreaks == 3)
                {
                    length = i;
                    break;
                }
            var text = result.StatusText + "\n" + details.Substring(0, length);
            if (result.IsReady) _notifications.Info(text, 6f);
            else _notifications.Warning(text, 6f);
            details = details.Substring(length).TrimStart();
        }
    }

    public DroneReadinessResult GetResult(string droneId) =>
        droneId != null && _results.TryGetValue(droneId, out var result) ? result : null;

    public bool IsValidated(string droneId) => GetResult(droneId)?.IsReady == true;

    public DroneReadinessResult Validate(string droneId)
    {
        if (!_assembly.TryGetDroneForPart(droneId, out var drone) || drone.InstanceId != droneId)
            return null;
        var root = _assembly.GetViewById(droneId);
        if (root == null) return null;
        // Revoke the old result before running checks, including when a validator throws.
        _results.Remove(droneId);
        Changed?.Invoke();
        var parts = drone.partInstanseIds.Select(_assembly.GetPartDomainState).ToList();
        var result = _readiness.Validate(new DroneValidationContext
        {
            Drone = drone,
            Parts = parts,
            PartsByType = parts.GroupBy(p => _configs.Get(p.PartId).PartType)
                .ToDictionary(g => g.Key, g => g.ToList()),
            droneTransform = root.transform,
            Requirements = new DroneRequirements
            {
                MinFlightTimeMinutes = 4f,
                MinThrustToWeightRatio = 1.8f,
                MaxCenterOfMassOffset = 0.2f,
                RequireCamera = true,
                CheckCollisions = true
                
            }
        });
        _results[droneId] = result;
        Changed?.Invoke();
        return result;
    }

    public void Invalidate()
    {
        if (_results.Count == 0) return;
        _results.Clear();
        Changed?.Invoke();
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
        _results.Clear();
    }
}
