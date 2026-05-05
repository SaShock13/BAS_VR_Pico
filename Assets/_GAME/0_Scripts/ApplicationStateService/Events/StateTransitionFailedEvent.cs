using System;

internal class StateTransitionFailedEvent : IAppEvent
{
    public AppState FromState { get; set; }
    public AppState ToState { get; set; }
    public Exception Error { get; set; }
    public StateTransitionContext Context { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "StateTransitionFailedEvent";
}