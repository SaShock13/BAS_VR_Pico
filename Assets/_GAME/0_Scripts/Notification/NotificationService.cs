using System;
using UnityEngine;

public sealed class NotificationService
    : INotificationService
{
    public event Action<ToastNotification> ToastRequested;

    public event Action<WorldNotification> WorldRequested;

    public void Success(string text, SFXId? sound = null, float duration = 3f,
        Action onDismissed = null, Action onCancelled = null)
    {
        if (ToastRequested == null)
        {
            onCancelled?.Invoke();
            return;
        }
        ToastRequested.Invoke(new ToastNotification(text, NotificationType.Info, duration, sound,
            onDismissed, onCancelled));
    }

    public void Info(string text,float duration = 3f)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Info,
                duration));
    }


    public void Warning(string text, float duration = 4f)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Warning,
                duration));
    }

    public void Error(string text, float duration = 5f)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Error,
                duration));
    }

    public void ShowWorld(
        string text,
        Transform anchor,
        NotificationType type = NotificationType.Info,
        float duration = 3f)
    {
        WorldRequested?.Invoke(
            new WorldNotification(
                text,
                type,
                anchor,
                duration));
    }
}
