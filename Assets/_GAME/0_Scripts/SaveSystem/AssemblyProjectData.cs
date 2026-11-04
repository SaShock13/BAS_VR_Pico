using System;

[Serializable]
public class AssemblyProjectData
{
    public int Version = 1;
    public string Id;
    public string Name;
    public long CreatedAtTicks;
    public long UpdatedAtTicks;
    public AssemblySaveData Assembly;
}
