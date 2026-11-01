using System.Collections.Generic;
using UnityEngine;

public class PartConfigRepository : IPartConfigRepository
{

    private readonly Dictionary<string, PartConfig> _configs;

    public PartConfigRepository(IEnumerable<PartConfig> configs)
    {
        _configs = new Dictionary<string, PartConfig>();
        foreach (var config in configs)
            _configs[config.PartId] = config;
        // Canonical IDs take precedence over aliases from older saves.
        foreach (var config in new HashSet<PartConfig>(_configs.Values))
        {
            if (config.LegacyPartIds == null) continue;
            foreach (var alias in config.LegacyPartIds)
                if (!string.IsNullOrEmpty(alias) && !_configs.ContainsKey(alias))
                    _configs.Add(alias, config);
        }
    }

    public PartConfig Get(string partId)
    {
        return _configs[partId];
    }
}
