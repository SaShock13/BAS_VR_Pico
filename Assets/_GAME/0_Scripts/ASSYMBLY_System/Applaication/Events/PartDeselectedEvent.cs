using System;

public class PartDeselectedEvent : IAppEvent
{
    public string InstanceId { get; set; }

    public string EventId { get; set; } = "PartDeselectedEvent";

    public DateTime Timestamp { get; set; }

}