using Pico.Platform;
using UnityEngine;
using Zenject;

public class NotificationTEST : MonoBehaviour
{
   
    [Inject] private INotificationService _notifications;

    void Update()
    {


        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            _notifications.Info("Информационное сообщение тестовое");
        }


        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            _notifications.ShowWorld("Батарея разряжена", transform, NotificationType.Warning);
        }

    }
}
