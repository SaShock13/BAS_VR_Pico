using System;
using UnityEngine;

public class Clean_DuiblicatePartRequest : IAppEvent
{
    public string InstanceId;

    public string EventId { get; set; } = "Clean_DuiblicatePartRequest";

    public DateTime Timestamp { get; set; }
}