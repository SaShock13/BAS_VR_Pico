using System;
using UnityEngine;

public struct ArmCoverScaleChangedEvent : IAppEvent
{
    public string EventId => nameof(ArmCoverScaleChangedEvent);
    public DateTime Timestamp { get; set; }
    public string InstanceId;
    public Vector3 Scale;
}
