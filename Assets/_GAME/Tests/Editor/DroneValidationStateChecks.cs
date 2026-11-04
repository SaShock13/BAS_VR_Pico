#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

public static class DroneValidationStateChecks
{
    [MenuItem("Tools/BAS VR/Check validation state")]
    public static void Run()
    {
        var bus = new TestBus();
        var state = new DroneValidationStateService(bus, null, null, null, new NotificationService());
        state.Initialize();
        var results = (Dictionary<string, DroneReadinessResult>)typeof(DroneValidationStateService)
            .GetField("_results", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(state);
        Check(!state.IsValidated("a"), "Unchecked drone accepted");
        results["a"] = new DroneReadinessResult { IsReady = false };
        Check(!state.IsValidated("a"), "Failed drone accepted");
        results["a"] = new DroneReadinessResult { IsReady = true };
        Check(state.IsValidated("a") && !state.IsValidated("b"), "Drone identity gate failed");
        bus.Publish(new DronesRebuiltEvent());
        Check(!state.IsValidated("a"), "Rebuild did not revoke validation");
        results["a"] = new DroneReadinessResult { IsReady = true };
        bus.Publish(new AssemblyGeometryChangedEvent());
        Check(!state.IsValidated("a"), "Geometry did not revoke validation");
        results["a"] = new DroneReadinessResult { IsReady = true };
        bus.Publish(new PartVisualChangedEvent("part", default));
        Check(!state.IsValidated("a"), "Visual edit did not revoke validation");
        CheckLaunchWarning(bus, state);
        CheckValidationToasts();
        state.Dispose();
        Check(bus.Count == 0, "Subscription leak");
        UnityEngine.Debug.Log("PASS: validation state, drone identity, rebuild/geometry/visual invalidation and subscription disposal");
    }

    private static void Check(bool condition, string error)
    {
        if (!condition) throw new Exception(error);
    }

    private static void CheckLaunchWarning(TestBus bus, DroneValidationStateService state)
    {
        var selection = new SelectionService { Current = new SelectionTarget(SelectionType.Drone, "a") };
        var assembly = new Clean_AssemblySystem(bus, null, null, null, null, null, null, selection, null, null);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var parts = (Dictionary<string, PartDomainState>)typeof(Clean_AssemblySystem).GetField("_parts", flags).GetValue(assembly);
        var drones = (Dictionary<string, DroneDomainState>)typeof(Clean_AssemblySystem).GetField("_drones", flags).GetValue(assembly);
        parts["a"] = new PartDomainState("a", "test", DroneAssemblyRules.RootPartType, default) { DroneId = "a" };
        var drone = new DroneDomainState("a");
        drone.partInstanseIds.Add("a");
        drones["a"] = drone;
        var notifications = new NotificationService();
        ToastNotification? received = null;
        notifications.ToastRequested += message => received = message;
        using (var launch = new FlightLaunchService(bus, selection, assembly, notifications, state))
        {
            launch.Initialize();
            bus.Publish(new LaunchFlightRequest());
            Check(received.HasValue && received.Value.Type == NotificationType.Warning
                && received.Value.Text.Contains("Валидация не пройдена"), "Missing validation ToastNotification");
            // A second failed attempt must still report the warning rather than lock launch.
            received = null;
            bus.Publish(new LaunchFlightRequest());
            Check(received.HasValue, "Repeated rejected launch lost its toast");
        }
    }

    private static void CheckValidationToasts()
    {
        var notifications = new NotificationService();
        var toasts = new List<ToastNotification>();
        notifications.ToastRequested += toasts.Add;
        using var state = new DroneValidationStateService(new TestBus(), null, null, null, notifications);
        var show = typeof(DroneValidationStateService).GetMethod("ShowValidationResult", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = new DroneReadinessResult { IsReady = true };
        show.Invoke(state, new object[] { result });
        Check(toasts.Count == 1 && toasts[0].Type == NotificationType.Info
            && toasts[0].Text == result.StatusText, "Missing success toast");
        toasts.Clear();
        result.IsReady = false;
        var group = new ValidationGroupResult();
        group.Messages.Add(new ValidationMessage(ValidationSeverity.Error, "Отсутствует аккумулятор"));
        group.Messages.Add(new ValidationMessage(ValidationSeverity.Warning, "Недостаточная тяга"));
        group.Messages.Add(new ValidationMessage(ValidationSeverity.Info, "Служебная информация"));
        result.Groups.Add(group);
        show.Invoke(state, new object[] { result });
        Check(toasts.Count == 1 && toasts[0].Type == NotificationType.Warning
            && toasts[0].Text == result.StatusText + "\n" + result.GetDetailsText()
            && !toasts[0].Text.Contains("Служебная информация"), "Toast differs from panel details");
        toasts.Clear();
        group.Messages.Clear();
        var longReason = new string('x', 500);
        group.Messages.Add(new ValidationMessage(ValidationSeverity.Error, longReason));
        show.Invoke(state, new object[] { result });
        var reconstructed = "";
        foreach (var toast in toasts)
        {
            Check(toast.Text.Length <= result.StatusText.Length + 141, "Toast page too long");
            reconstructed += toast.Text.Substring(result.StatusText.Length + 1);
        }
        Check(toasts.Count > 1 && reconstructed == longReason, "Long validation reason was lost");
    }

    private sealed class TestBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();
        public int Count => _handlers.Count;
        public void Initialize() { }
        public void Dispose() => _handlers.Clear();
        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IAppEvent
        {
            _handlers.Add(typeof(T), handler);
            return new Cleanup(() => _handlers.Remove(typeof(T)));
        }
        public IDisposable Subscribe<T>(Action<T> handler, Func<T, bool> filter, EventPriority priority = EventPriority.Normal) where T : IAppEvent
            => Subscribe<T>(e => { if (filter == null || filter(e)) handler(e); }, priority);
        public void Publish<T>(T value) where T : IAppEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var handler)) ((Action<T>)handler)(value);
        }
        public Task PublishAsync<T>(T value, CancellationToken token = default) where T : IAppEvent
        {
            Publish(value);
            return Task.CompletedTask;
        }
        public int GetSubscriberCount<T>() where T : IAppEvent => _handlers.ContainsKey(typeof(T)) ? 1 : 0;
    }

    private sealed class Cleanup : IDisposable
    {
        private readonly Action _action;
        public Cleanup(Action action) { _action = action; }
        public void Dispose() => _action();
    }
}
#endif
