using System.Collections.Generic;

public interface IMaterialRegistry
{
    MaterialDefinition Get(string id);

    IReadOnlyList<MaterialDefinition> GetAll();
}