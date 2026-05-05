using System;

internal class CriticalErrorEvent : IAppEvent
{
    public Exception Error { get; set; }
    public string Context { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "CriticalErrorEvent";
}