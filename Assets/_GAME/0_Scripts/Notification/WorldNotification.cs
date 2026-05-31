using UnityEngine;

public readonly struct WorldNotification
{
    public readonly string Text;
    public readonly NotificationType Type;
    public readonly Transform Anchor;
    public readonly float Duration;

    public WorldNotification(
        string text,
        NotificationType type,
        Transform anchor,
        float duration)
    {
        Text = text;
        Type = type;
        Anchor = anchor;
        Duration = duration;
    }
}