using System;

public enum ControllerHand
{
    Left,Right,Both
}
public sealed class HapticFeedbackRequestedEvent : IAppEvent
{
    public float Amplitude { get; set; }
    public float Duration { get; set; }
    public ControllerHand Hand { get; set; }
    public DateTime Timestamp { get; set; }

    public string EventId { get; set; } = "HapticFeedbackRequestedEvent";
}
