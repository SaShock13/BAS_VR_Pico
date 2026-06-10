using System;
using UnityEngine;

public interface INotificationService
{
    event Action<ToastNotification> ToastRequested;

    event Action<WorldNotification> WorldRequested;

    void Info(string text, float duration = 3f);

    void Warning(string text, float duration = 4f);

    void Error(string text, float duration = 5f);

    void ShowWorld(
        string text,
        Transform anchor,
        NotificationType type = NotificationType.Info,
        float duration = 3f);
}