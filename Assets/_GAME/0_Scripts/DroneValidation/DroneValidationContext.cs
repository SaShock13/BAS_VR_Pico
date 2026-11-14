using System.Collections.Generic;
using UnityEngine;

public sealed class DroneValidationContext
{
    public DroneDomainState Drone;

    public DroneRequirements Requirements;
    public bool EnforceMissionLimits;

    public Transform droneTransform;

    public List<PartDomainState> Parts;

    public Dictionary<PartType, List<PartDomainState>>
        PartsByType = new();

    public DronePhysicsData physicsData;
}
