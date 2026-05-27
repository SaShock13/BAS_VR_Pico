using System;
using UnityEngine;

public class PartSocketDetachedEvent : IAppEvent
{
    //public PartChildable AttachedPard { get; set; } = null;
    public string EventId => "PartSocketDetachedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }
}

