using System;

public class MenuNavigationStatusEvent : IAppEvent
{
    public string EventId => nameof(MenuNavigationStatusEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsLoading { get; set; }
    public string Error { get; set; }
}
