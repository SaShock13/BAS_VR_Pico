using System.Collections.Generic;
using System.Linq;
using Zenject;

public sealed class DroneAnalyzer : IDroneAnalyzer
{
    [Inject] private readonly Clean_AssemblySystem _assembly;


    public bool HasPart(
        DroneDomainState drone,
        PartType type)
    {
        return drone.partInstanseIds
            .Select(id =>
                _assembly.GetPartDomainState(id))
            .Any(part =>
                part.Type == type);
    }

    public int CountParts(
        DroneDomainState drone,
        PartType type)
    {
        return drone.partInstanseIds
            .Select(id =>
                _assembly.GetPartDomainState(id))
            .Count(part =>
                part.Type == type);
    }

    public IReadOnlyList<PartDomainState> GetParts(
        DroneDomainState drone,
        PartType type)
    {
        return drone.partInstanseIds
            .Select(id =>
                _assembly.GetPartDomainState(id))
            .Where(part =>
                part.Type == type)
            .ToList();
    }
}