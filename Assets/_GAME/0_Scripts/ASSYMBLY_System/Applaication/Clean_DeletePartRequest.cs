
using System;

public class Clean_DeletePartRequest : IAppEvent
{
    public string InstanceId;

    public string EventId { get; set; } = "Clean_DeletePartRequest";

    public DateTime Timestamp { get; set; }
}