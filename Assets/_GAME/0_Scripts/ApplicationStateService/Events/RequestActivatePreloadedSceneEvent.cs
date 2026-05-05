using System;
using UnityEngine;

public class RequestActivatePreloadedSceneEvent : IAppEvent
{
    public string EventId { get; set; } = "RequestActivatePreloadedSceneEvent";

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
