using System.Collections.Generic;

public interface IDroneAnalyzer
{
    bool HasPart(
        DroneValidationContext context,
        PartType type);

    int CountParts(
        DroneValidationContext context,
        PartType type);

    IReadOnlyList<PartDomainState> GetParts(
        DroneValidationContext context,
        PartType type);

    IReadOnlyList<string> FindCollisions(
        DroneDomainState drone);
}