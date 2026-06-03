using System.Collections.Generic;

public sealed class DroneValidationContext
{
    public DroneDomainState Drone;

    public DroneRequirements Requirements;

    public List<PartDomainState> Parts;

    public Dictionary<PartType, List<PartDomainState>>
        PartsByType = new();
}