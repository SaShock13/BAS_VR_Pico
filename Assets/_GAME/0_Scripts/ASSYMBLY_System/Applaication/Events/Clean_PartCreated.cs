using System;
using UnityEngine;

public class Clean_PartCreated : IAppEvent
{
    public string InstanceId;

    public string EventId { get; set; } = "Clean_PartCantBeDeletedEvent";

    public DateTime Timestamp { get; set; }
}