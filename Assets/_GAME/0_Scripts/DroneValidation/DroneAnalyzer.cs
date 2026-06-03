using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

public sealed class DroneAnalyzer : IDroneAnalyzer
{
    [Inject] private readonly Clean_AssemblySystem _assembly;
    [Inject] private readonly IPartConfigRepository _configs;
    [Inject] private readonly IAppLogger _logger;



    //public bool HasPart(
    //    DroneDomainState drone,
    //    PartType type)
    //{

    //    _configs.Get
    //    return drone.partInstanseIds
    //        .Select(id =>
    //            _assembly.GetPartDomainState(id))
    //        .Any(part =>
    //            part.Type == type);
    //}

    //public int CountParts(
    //    DroneDomainState drone,
    //    PartType type)
    //{
    //    return drone.partInstanseIds
    //        .Select(id =>
    //            _assembly.GetPartDomainState(id))
    //        .Count(part =>
    //            part.Type == type);
    //}

    //public IReadOnlyList<PartDomainState> GetParts(
    //    DroneDomainState drone,
    //    PartType type)
    //{
    //    return drone.partInstanseIds
    //        .Select(id =>
    //            _assembly.GetPartDomainState(id))
    //        .Where(part =>
    //            part.Type == type)
    //        .ToList();
    //}


    public bool HasPart(
    DroneValidationContext context, 
    PartType type)
    {
        return context.PartsByType.ContainsKey(type);
    }


    public int CountParts(
    DroneValidationContext context,
    PartType type)
    {
        return context.PartsByType
            .TryGetValue(type, out var parts)
                ? parts.Count
                : 0;
    }

    public IReadOnlyList<PartDomainState> GetParts(
    DroneValidationContext context,
    PartType type)
    {
        return context.PartsByType
            .TryGetValue(type, out var parts)
                ? parts
                : Array.Empty<PartDomainState>();
    }


    public IReadOnlyList<string> FindCollisions(DroneDomainState drone)
    {
        return new List<string>();
    }
}