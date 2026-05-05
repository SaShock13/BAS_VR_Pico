using System;
using UnityEngine;

public class PartFreeAttachedEvent : IAppEvent
{

    //public PartChildable AttachedPard { get; set; } = null;
    public string EventId { get; set; } = "PartFreeAttachedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }
}