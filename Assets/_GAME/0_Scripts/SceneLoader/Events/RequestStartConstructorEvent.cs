using System;

public class RequestStartConstructorEvent : IAppEvent
{
    public string EventId => nameof(RequestStartConstructorEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
