using System.Collections.Generic;

public interface IVisualPresetRepository
{
    void Save(VisualPreset preset);
    void Remove(string id);

    VisualPreset Get(string id);

    IReadOnlyList<VisualPreset> GetAll();
}