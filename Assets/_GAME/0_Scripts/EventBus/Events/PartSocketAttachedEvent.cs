using System;
using UnityEngine;

public class PartSocketAttachedEvent : IAppEvent
{
    public string PartInstanceId;
    public string AttachedPartId;
    public string AttachedSocketId;

    //public PartChildable AttachedPard { get; set; } = null;
    public string EventId => "PartSocketAttachedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }
}
