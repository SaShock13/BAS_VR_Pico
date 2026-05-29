using System;

[Serializable]
public class GarageDroneData
{
    public string GarageId;

    public string DroneId;

    public string DroneName;

    public long CreatedAtTicks;

    public AssemblySaveData Assembly;
}