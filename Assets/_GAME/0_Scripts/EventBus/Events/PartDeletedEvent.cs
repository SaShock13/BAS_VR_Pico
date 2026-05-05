using System;
using UnityEngine;

public class PartDeletedEvent :IAppEvent
{    
    //public DronePart DeletedPart { get; set; } = null;
    public string EventId => "PartDeletedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }

}
