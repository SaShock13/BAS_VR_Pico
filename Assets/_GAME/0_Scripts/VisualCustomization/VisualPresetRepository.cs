using System.Collections.Generic;
using System.Linq;

public class VisualPresetRepository
    : IVisualPresetRepository
{
    public event System.Action Changed;
    private readonly Dictionary<string, VisualPreset> _presets
        = new();

    public void Save(VisualPreset preset)
    {
        _presets[preset.Id] = preset;
        Changed?.Invoke();
    }
    public void Remove(string id)
    {
        if (_presets.Remove(id)) Changed?.Invoke();
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
