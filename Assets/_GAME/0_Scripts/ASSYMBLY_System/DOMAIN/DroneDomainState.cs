using System.Collections.Generic;

public class DroneDomainState
{
    public string InstanceId { get; }
    public string Name { get; }
    public float TotalMass { get; internal set; }

    public List<string> partInstanseIds ;
    public DroneDomainState(string instanceId)
    {
        InstanceId = instanceId;
        partInstanseIds = new();
        Name = "Простодрон";
    }
}