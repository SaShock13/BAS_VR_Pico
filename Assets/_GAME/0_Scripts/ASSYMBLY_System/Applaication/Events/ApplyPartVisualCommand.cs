using System;

public class ApplyPartVisualCommand : IAppEvent
{
    public string InstanceId { get; private set; }

    public string EventId { get; set; } = "ApplyPartVisualCommand";

    public DateTime Timestamp { get; set; }

    public PartVisualProperties Visual;

    public ApplyPartVisualCommand(string instanceId, PartVisualProperties visual)
    {
        InstanceId = instanceId;
        Visual = visual;
        Timestamp = DateTime.UtcNow;
    }
}