using System.Collections.Generic;
using UnityEngine;

public class DronePhysicsData
{
    public float TotalMass;

    public Vector3 CenterOfMass;

    public List<MotorPhysicsData> Motors;

    public BatteryPhysicsData Battery;

    public float MaxAvailableThrust;

    public float HoverThrottle;
}