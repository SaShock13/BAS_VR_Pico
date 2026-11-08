using System;

// A valid handle stops one voice; otherwise stops all voices with SFXId.
public sealed class StopSFXRequest : IAppEvent
{
    public string EventId => nameof(StopSFXRequest);
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public SFXHandle Handle { get; set; }
    public SFXId SFXId { get; set; }
}

