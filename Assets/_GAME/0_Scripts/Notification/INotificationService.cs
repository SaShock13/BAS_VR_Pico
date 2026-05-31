using System;
using UnityEngine;

public interface INotificationService
{
    event Action<ToastNotification> ToastRequested;

    event Action<WorldNotification> WorldRequested;

    void Info(string text);

    void Warning(string text);

    void Error(string text);

    void ShowWorld(
        string text,
        Transform anchor,
        NotificationType type = NotificationType.Info,
        float duration = 3f);
}