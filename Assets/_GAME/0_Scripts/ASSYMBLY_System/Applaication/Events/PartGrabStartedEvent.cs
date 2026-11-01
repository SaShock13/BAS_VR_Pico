using System;

public class PartGrabStartedEvent : IAppEvent
{
    public string EventId => nameof(PartGrabStartedEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string InstanceId { get; set; }
}
