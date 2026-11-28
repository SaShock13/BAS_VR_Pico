using System;

public sealed class GaragePortalRequest : IAppEvent
{
    public string EventId => nameof(GaragePortalRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool ReturnToAssembly;
}
