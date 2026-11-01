using System;

public class SetAssemblyDetailModeRequest : IAppEvent
{
    public string EventId => nameof(SetAssemblyDetailModeRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsHeld { get; set; }
}
