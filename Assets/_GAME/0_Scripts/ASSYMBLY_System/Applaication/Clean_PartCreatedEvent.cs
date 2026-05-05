using System;
using UnityEngine;

public class Clean_PartCreatedEvent:IAppEvent
{
    public string InstanceId;

    public GameObject GameObject;

    public string EventId { get; set; } = "Clean_PartCreatedEvent";

    public DateTime Timestamp { get; set; }
}
