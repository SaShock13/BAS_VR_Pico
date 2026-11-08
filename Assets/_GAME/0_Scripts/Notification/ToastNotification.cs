public readonly struct ToastNotification
{
    public readonly string Text;
    public readonly NotificationType Type;
    public readonly float Duration;
    public readonly SFXId? Sound;
    public readonly System.Action OnDismissed;
    public readonly System.Action OnCancelled;

    public ToastNotification(
        string text,
        NotificationType type,
        float duration,
        SFXId? sound = null, System.Action onDismissed = null, System.Action onCancelled = null)
    {
        Text = text;
        Type = type;
        Duration = duration;
        Sound = sound;
        OnDismissed = onDismissed;
        OnCancelled = onCancelled;
    }
}
