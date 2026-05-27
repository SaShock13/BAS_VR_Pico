using System;

public class PartSocketDetachRequest : IAppEvent
{
    public string PartInstanceId;
    //public string AttachedPartId;
    //public string AttachedSocketId;

    public string EventId { get; set; } = "PartSocketDetachRequest";

    public DateTime Timestamp { get; set; }
}
