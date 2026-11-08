using UnityEngine;

// Environment scenes have no DI container or XR rig.
public sealed class FlightEnvironmentContext : MonoBehaviour
{
    public Transform playerSpawn;
    public Transform droneSpawn;
}
