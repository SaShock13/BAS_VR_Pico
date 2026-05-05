using System;

public class Clean_PartCantBeDeletedEvent : IAppEvent
{
    public string InstanceId;

    public string EventId { get; set; } = "Clean_PartCantBeDeletedEvent";

    public DateTime Timestamp { get; set; }
}
