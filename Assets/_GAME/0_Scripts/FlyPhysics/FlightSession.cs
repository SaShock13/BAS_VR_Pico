/// <summary>
/// Временный обьект для передачи дрона между сценами
/// </summary>
public static class FlightSession  
{
    public static string EnvironmentId;
    public static AssemblySaveData Drone;
    public static DronePhysicsData Physics;
    public static FlightMode Mode = FlightMode.Arcade;
}
