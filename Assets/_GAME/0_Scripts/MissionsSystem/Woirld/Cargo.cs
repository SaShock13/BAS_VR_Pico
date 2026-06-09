using UnityEngine;

public class Cargo : MonoBehaviour
{
    public void PickUp()
    {
        MissionEvents.CargoPickedUp?.Invoke();
    }

    public void Deliver()
    {
        MissionEvents.CargoDelivered?.Invoke();
    }
}