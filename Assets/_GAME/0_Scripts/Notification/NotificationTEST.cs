using Pico.Platform;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class NotificationTEST : MonoBehaviour
{
   
    [Inject] private INotificationService _notifications;

    void Update()
    {


        if (Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            _notifications.Info("Информационное сообщение тестовое");
        }


        if (Keyboard.current != null && Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            _notifications.ShowWorld("Батарея разряжена", transform, NotificationType.Warning);
        }

    }
}
