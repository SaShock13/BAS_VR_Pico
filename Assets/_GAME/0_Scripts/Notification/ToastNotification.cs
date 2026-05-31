public readonly struct ToastNotification
{
    public readonly string Text;
    public readonly NotificationType Type;
    public readonly float Duration;

    public ToastNotification(
        string text,
        NotificationType type,
        float duration )
    {
        Text = text;
        Type = type;
        Duration = duration;
    }
}