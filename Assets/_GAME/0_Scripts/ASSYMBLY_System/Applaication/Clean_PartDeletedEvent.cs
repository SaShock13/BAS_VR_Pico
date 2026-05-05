using System;
using UnityEngine;

public class Clean_PartDeletedEvent : IAppEvent
{
    public string InstanceId;


    public string EventId { get; set; } = "Clean_PartDeletedEvent";

    public DateTime Timestamp { get; set; }
}
