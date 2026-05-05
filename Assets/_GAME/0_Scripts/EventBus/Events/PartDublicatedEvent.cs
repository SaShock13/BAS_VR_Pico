using System;
using UnityEngine;

public class PartDublicatedEvent:IAppEvent
{
    //public DronePart OriginPart { get; set; } = null;
    public string EventId => "PartDeletedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }
}
