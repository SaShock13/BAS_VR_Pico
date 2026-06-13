using System.Collections.Generic;
using System.Linq;

public class MaterialRegistry : IMaterialRegistry
{
    private readonly Dictionary<string, MaterialDefinition> _materials;

    public MaterialRegistry(
        IEnumerable<MaterialDefinition> materials)
    {
        _materials = materials.ToDictionary(x => x.Id);
    }

    public MaterialDefinition Get(string id)
    {
        return _materials[id];
    }

    public IReadOnlyList<MaterialDefinition> GetAll()
    {
        return _materials.Values.ToList();
    }
}