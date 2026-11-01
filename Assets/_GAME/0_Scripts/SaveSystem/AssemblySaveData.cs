using System;
using System.Collections.Generic;

[Serializable]
public class AssemblySaveData
{
    public int Version = 2;
    public List<PartSaveData> Parts = new();
}
