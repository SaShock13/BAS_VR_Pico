using UnityEngine;

// Environment scenes have no DI container or XR rig.
public sealed class FlightEnvironmentContext : MonoBehaviour
{
    public Transform playerSpawn;
    public Transform droneSpawn;
    public Transform missionStart;
    [Min(0.1f)] public float missionStartRadius = 1.5f;
    public MissionPoint[] missionPoints;
    public Cargo cargo;
}
