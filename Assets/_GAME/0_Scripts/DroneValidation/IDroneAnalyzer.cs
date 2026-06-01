using System.Collections.Generic;

public interface IDroneAnalyzer
{
    bool HasPart(
        DroneDomainState drone,
        PartType type);

    int CountParts(
        DroneDomainState drone,
        PartType type);

    IReadOnlyList<PartDomainState> GetParts(
        DroneDomainState drone,
        PartType type);
}