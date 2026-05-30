using System.Collections.Generic;

public interface IGarageService
{

    bool HasFreeSlot();

    IReadOnlyList<GarageDroneData> GetAll();

    GarageDroneData Get(string garageId);
    public GarageDroneData GetByDroneId(string droneId);
    void SaveDrone(string droneId);

    void OverwriteDrone(
        string garageId,
        string droneId);

    void Delete(string garageId);

    bool Contains(string garageId);

    void Clear();
}