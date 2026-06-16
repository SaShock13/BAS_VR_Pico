using System.Collections.Generic;
using System.Linq;

public class VisualPresetRepository
    : IVisualPresetRepository
{
    private readonly Dictionary<string, VisualPreset> _presets
        = new();

    public void Save(VisualPreset preset)
    {
        _presets[preset.Id] = preset;
    }
    public void Remove(string id)
    {
        _presets.Remove(id);
    }


    public VisualPreset Get(string id)
    {
        return _presets[id];
    }

    public IReadOnlyList<VisualPreset> GetAll()
    {
        return _presets.Values.ToList();
    }
}