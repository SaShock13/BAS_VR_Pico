using System;
using UnityEngine;

public struct SetArmCoverScaleRequest : IAppEvent
{
    public string EventId => nameof(SetArmCoverScaleRequest);
    public DateTime Timestamp { get; set; }
    public string InstanceId;
    public Vector3 Scale;
    public bool ResetToDefault;
}
