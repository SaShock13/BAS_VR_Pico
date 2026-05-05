using System;

public class PauseStateChangedEvent : IAppEvent
{
    public bool IsPaused { get; set; }
    public PauseInitiator Reason { get; set; }
    public AppState PausedState { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "PauseStateChangedEvent";
    public TimeSpan PauseDuration { get; internal set; }
}