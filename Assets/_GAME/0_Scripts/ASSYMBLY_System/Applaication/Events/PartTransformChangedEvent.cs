using System;
using UnityEngine;

public class PartTransformChangedEvent : IAppEvent
{
    public string instanceId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 StartPosition;
    public Quaternion StartRotation;

    public string EventId { get; set; } = "PartTransformChangedEvent";

    public DateTime Timestamp { get; set; }
}
