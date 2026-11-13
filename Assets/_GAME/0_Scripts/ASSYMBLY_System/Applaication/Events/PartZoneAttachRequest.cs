using System;

public sealed class PartZoneAttachRequest : IAppEvent
{
    public string EventId => nameof(PartZoneAttachRequest);
    public DateTime Timestamp { get; set; }
    public string PartInstanceId;
    public string ParentInstanceId;
    public string MountId;
    public bool RestorePreviousPosition;
}
