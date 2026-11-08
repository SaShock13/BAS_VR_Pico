using System;

public sealed class MissionCompletionFeedbackEvent : IAppEvent
{
    public string EventId => nameof(MissionCompletionFeedbackEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; }
    public SFXId? Sound { get; }
    public bool IsMissionComplete { get; }
    public float Duration { get; }
    public Action OnDismissed { get; }
    public Action OnCancelled { get; }

    public MissionCompletionFeedbackEvent(string message, SFXId? sound, bool isMissionComplete,
        float duration = 0f, Action onDismissed = null, Action onCancelled = null)
    {
        Message = message;
        Sound = sound;
        IsMissionComplete = isMissionComplete;
        Duration = duration;
        OnDismissed = onDismissed;
        OnCancelled = onCancelled;
    }
}
