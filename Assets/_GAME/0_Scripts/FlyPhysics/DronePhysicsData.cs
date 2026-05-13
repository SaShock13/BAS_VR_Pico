using System.Collections.Generic;
using UnityEngine;

public class DronePhysicsData
{
    public float TotalMass;

    public Vector3 LocalCenterOfMass;

    public IReadOnlyList<MotorPhysicsData> Motors;

    public BatteryPhysicsData Battery;

    public float MaxAvailableThrust;

    public float HoverThrottle;
}