using UnityEngine;
using Zenject;

public class ValidationController : MonoBehaviour
{
    [Inject] private IEventBus _eventBus;

    public void ValidateDrone(string droneId)
    {
        _eventBus.Publish(new ValidateDroneRequest { DroneId = droneId });
    }
}
