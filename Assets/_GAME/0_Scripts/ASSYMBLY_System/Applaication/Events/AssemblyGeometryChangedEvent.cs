using System;

public class AssemblyGeometryChangedEvent : IAppEvent
{
    public string EventId => nameof(AssemblyGeometryChangedEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
