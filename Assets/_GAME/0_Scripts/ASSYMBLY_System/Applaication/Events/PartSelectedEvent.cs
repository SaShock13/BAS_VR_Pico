using System;
using UnityEngine;

public class PartSelectedEvent : IAppEvent
{
    public string InstanceId { get; set; }

    public string EventId { get; set; } = "PartSelectedEvent";

    public DateTime Timestamp { get; set; }


    public PartSelectedEvent(string instanceId)
    {
        InstanceId = instanceId;
    }
}
