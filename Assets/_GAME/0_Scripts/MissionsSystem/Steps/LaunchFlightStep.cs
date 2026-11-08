using System;
using UnityEngine;

public sealed class LaunchFlightStep : MissionStep
{
    private readonly IEventBus _events;
    private readonly IHintScenarioController _hints;
    private readonly Transform _target;
    private readonly string _description;
    private IDisposable _subscription;
    public override string Description => _description;
    public override Transform Target => _target;

    public LaunchFlightStep(IEventBus events, IHintScenarioController hints,
        Transform target, string description)
    {
        _events = events;
        _hints = hints;
        _target = target;
        _description = description;
    }

    public override void Enter()
    {
        _subscription = _events.Subscribe<FlightLaunchApprovedEvent>(OnLaunchApproved);
        _hints.StartScenario(new HintContext(Description, _target, pointAtTarget: true));
    }

    private void OnLaunchApproved(FlightLaunchApprovedEvent approved)
    {
        if (IsCompleted || !approved.TryDefer())
            return;
        Context.CompletionNotificationDismissed = approved.Continue;
        Context.CompletionNotificationCancelled = approved.Cancel;
        Complete();
    }

    public override void Exit()
    {
        _subscription?.Dispose();
        _subscription = null;
        _hints.StopScenario();
    }
}
