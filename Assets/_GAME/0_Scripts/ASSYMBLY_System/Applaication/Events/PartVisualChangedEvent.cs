using System;
using UnityEngine;

public class PartVisualChangedEvent : IAppEvent
{
    public string InstanceId { get;  set; }

    public PartVisualProperties Visual { get; set; }

    public string EventId { get; set; } = "PartVisualChangedEvent";

    public DateTime Timestamp { get; set; }


    public PartVisualChangedEvent(string instanceId, PartVisualProperties visual)
    {
        InstanceId = instanceId;
        Visual = visual;
    }
}
