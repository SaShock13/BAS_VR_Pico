using System;

public class PartSocketAttachRequest : IAppEvent
{
    public string PartInstanceId;
    public string AttachedPartId;
    public string AttachedSocketId;

    public string EventId { get; set; } = "PartSocketAttachRequest";

    public DateTime Timestamp { get; set; }
}
