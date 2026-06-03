public sealed class DroneRequirements
{
    public int MinMotorCount;
    public int MinPropellerCount;

    public bool RequireBattery;
    public bool RequireFrame;
    public bool RequireCamera;

    public float MinFlightTimeMinutes;

    public float MinThrustToWeightRatio;

    public float MaxCenterOfMassOffset;

    public bool CheckCollisions = true;

}