using System;
using UnityEngine;

public class DroneSelectedEvent : IAppEvent
{
    public string InstanceId { get; set; }

    public string EventId { get; set; } = "DroneSelectedEvent";

    public DateTime Timestamp { get; set; }


    public DroneSelectedEvent(string instanceId)
    {
        InstanceId = instanceId;
    }
}
