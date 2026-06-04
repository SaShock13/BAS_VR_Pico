using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

public sealed class DroneAnalyzer : IDroneAnalyzer
{
    //private readonly Clean_AssemblySystem _assembly;
    //private readonly IPartConfigRepository _configs;
    //private readonly IAppLogger _logger;

    //public DroneAnalyzer(Clean_AssemblySystem assembly, IPartConfigRepository configs, IAppLogger logger)
    //{
    //    _assembly = assembly;
    //    _configs = configs;
    //    _logger = logger;
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