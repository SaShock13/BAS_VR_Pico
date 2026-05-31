using System;
using UnityEngine;

public sealed class NotificationService
    : INotificationService
{
    public event Action<ToastNotification> ToastRequested;

    public event Action<WorldNotification> WorldRequested;

    public void Info(string text)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Info,
                3f));
    }

    public void Warning(string text)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Warning,
                4f));
    }

    public void Error(string text)
    {
        ToastRequested?.Invoke(
            new ToastNotification(
                text,
                NotificationType.Error,
                5f));
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