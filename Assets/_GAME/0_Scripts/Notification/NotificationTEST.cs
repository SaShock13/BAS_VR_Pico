using Pico.Platform;
using UnityEngine;
using Zenject;

public class NotificationTEST : MonoBehaviour
{
   
    [Inject] private INotificationService _notifications;

    void Update()
    {


        if (Input.GetKeyDown(KeyCode.M))
        {
            _notifications.Info("Информационное сообщение тестовое");
        }


        if (Input.GetKeyDown(KeyCode.N))
        {
            _notifications.ShowWorld("Батарея разряжена", transform, NotificationType.Warning);
        }

    }
}
