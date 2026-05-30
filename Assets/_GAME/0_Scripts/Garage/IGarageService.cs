using System.Collections.Generic;

public interface IGarageService
{
    IReadOnlyList<GarageDroneData> GetAll();

    GarageDroneData Get(string garageId);

    void SaveDrone(string droneId);

    void OverwriteDrone(
        string garageId,
        string droneId);

    void Delete(string garageId);

    bool Contains(string garageId);

    void Clear();
}