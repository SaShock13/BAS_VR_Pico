
using System;

internal class StateChangeRequestedEvent : IAppEvent
{
    public AppState TargetState { get; set; }
    public StateTransitionContext Context { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "StateChangeRequestedEvent";
}
