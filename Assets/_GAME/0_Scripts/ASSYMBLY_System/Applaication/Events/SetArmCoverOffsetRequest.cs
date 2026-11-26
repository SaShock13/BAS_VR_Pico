using System;

public struct SetArmCoverOffsetRequest : IAppEvent
{
    public string EventId => nameof(SetArmCoverOffsetRequest);
    public DateTime Timestamp { get; set; }
    public string InstanceId;
    public float Offset;
}

public struct ArmCoverOffsetChangedEvent : IAppEvent
{
    public string EventId => nameof(ArmCoverOffsetChangedEvent);
    public DateTime Timestamp { get; set; }
    public string InstanceId;
}
