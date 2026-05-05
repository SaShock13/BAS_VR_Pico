using System;

public class ScenePreloadedEvent : IAppEvent
{
    public string EventId { get; set; } = "ScenePreloadedEvent";

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string PreloadedSceneName { get; set; }
}
