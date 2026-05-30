using System;

[Serializable]
public class GarageDroneData
{
    public string GarageId;

    public string DroneId;

    public DroneMetadata metaData;

    //public string DroneName;

    public long CreatedAtTicks;

    public AssemblySaveData Assembly;
}