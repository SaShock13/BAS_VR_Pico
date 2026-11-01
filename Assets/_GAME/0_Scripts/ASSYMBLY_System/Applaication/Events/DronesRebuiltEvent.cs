using System;

public class DronesRebuiltEvent : IAppEvent
{
    public string EventId { get; set; } = nameof(DronesRebuiltEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
