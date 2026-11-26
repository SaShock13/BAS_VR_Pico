using System;

public struct CommitArmCoverAdjustmentRequest : IAppEvent
{
    public string EventId => nameof(CommitArmCoverAdjustmentRequest);
    public DateTime Timestamp { get; set; }
    public string InstanceId;
}
