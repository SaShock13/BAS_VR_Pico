using System.Collections.Generic;

public interface IVisualPresetRepository
{
    void Save(VisualPreset preset);

    VisualPreset Get(string id);

    IReadOnlyList<VisualPreset> GetAll();
}