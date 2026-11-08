using System;
using UnityEngine;

public class Clean_PartDeletedEvent : IAppEvent
{
    public string InstanceId;
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; } = Vector3.one;


    public string EventId { get; set; } = "Clean_PartDeletedEvent";

    public DateTime Timestamp { get; set; }
}
