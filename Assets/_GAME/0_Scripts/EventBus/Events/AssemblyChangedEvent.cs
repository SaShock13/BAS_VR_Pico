
using System;
using UnityEngine;

public class AssemblyChangedEvent : IAppEvent
{
    //public DronePart DeletedPart { get; set; } = null;
    public string EventId => "AssemblyChangedEvent";
    public Vector3 Position { get; set; }

    public DateTime Timestamp { get; set; }
}