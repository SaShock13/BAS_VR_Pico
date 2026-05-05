using System;

public class AppStateChangedEvent : IAppEvent
{
    public AppState PreviousState { get; set; }
    public AppState NewState { get; set; }
    public TimeSpan TransitionDuration { get; set; }
    public StateTransitionContext Context { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "AppStateChangedEvent";
}