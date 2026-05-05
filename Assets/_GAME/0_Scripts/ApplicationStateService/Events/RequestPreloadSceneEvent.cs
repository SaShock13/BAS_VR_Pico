using System;

public class RequestPreloadSceneEvent : IAppEvent
{
    public string EventId { get; set; } = "RequestPreloadSceneEvent";

    public DateTime Timestamp { get ; set; } = DateTime.Now;

    public string TargetSceneName { get; set; }
}
