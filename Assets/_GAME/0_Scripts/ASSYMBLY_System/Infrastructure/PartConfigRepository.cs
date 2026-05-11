using System.Collections.Generic;
using UnityEngine;

public class PartConfigRepository : IPartConfigRegistry
{

    private readonly Dictionary<string, PartConfig> _configs;

    public PartConfigRepository(IEnumerable<PartConfig> configs)
    {
        _configs = new Dictionary<string, PartConfig>();
        foreach (var config in configs)
            _configs[config.PartId] = config;
    }

    public PartConfig Get(string partId)
    {
        return _configs[partId];
    }
}
