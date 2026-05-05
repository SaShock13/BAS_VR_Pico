using System;

internal class StateTransitionStartedEvent : IAppEvent
{
    public AppState FromState { get; set; }
    public AppState ToState { get; set; }
    public DateTime Timestamp { get; set; }
    public StateTransitionContext Context { get; set; }

    public string EventId { get; set; } = "StateTransitionStartedEvent";
}