using System.Collections.Generic;
using UnityEngine;

public sealed class DroneValidationContext
{
    public DroneDomainState Drone;

    public DroneRequirements Requirements;

    public Transform droneTransform;

    public List<PartDomainState> Parts;

    public Dictionary<PartType, List<PartDomainState>>
        PartsByType = new();

    public DronePhysicsData physicsData;
}