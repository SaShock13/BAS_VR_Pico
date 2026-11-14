/// <summary>
/// Временный обьект для передачи дрона между сценами
/// </summary>
public static class FlightSession  
{
    public static string EnvironmentId;
    public static MissionDefinition Mission;
    public static AssemblySaveData Drone;
    public static DronePhysicsData Physics;
    public static FlightMode Mode = FlightMode.Arcade;
    internal static event System.Action HudVisibilityChanged;
    private static int _hiddenHudFields;

    internal static bool IsHudFieldVisible(int index) => (_hiddenHudFields & (1 << index)) == 0;

    internal static void SetHudFieldVisible(int index, bool visible)
    {
        int bit = 1 << index;
        if (visible) _hiddenHudFields &= ~bit;
        else _hiddenHudFields |= bit;
        HudVisibilityChanged?.Invoke();
    }
}
