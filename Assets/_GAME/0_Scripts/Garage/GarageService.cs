using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GarageService : IGarageService
{
    private readonly Clean_AssemblySystem _assembly;


    private readonly IGarageSaveService _saveService;

    private GarageSaveData _garage;



    public GarageService(
        Clean_AssemblySystem assembly,
        IGarageSaveService saveService)
    {
        _assembly = assembly;
        _saveService = saveService;

        _garage = _saveService.Load() ?? new GarageSaveData();
    }



    public IReadOnlyList<GarageDroneData> GetAll()
    {
        return _garage.Drones;
    }



    public GarageDroneData Get(string garageId)
    {
        return _garage.Drones
            .FirstOrDefault(x => x.GarageId == garageId);
    }

    public GarageDroneData GetByDroneId(string droneId)
    {
        return _garage.Drones
            .FirstOrDefault(x => x.DroneId == droneId);
    }



    public void SaveDrone(string droneId)
    {
        Debug.Log($"gggggggg[GARAGE] SaveDrone {droneId}");

        AssemblySaveData assembly =
            _assembly.BuildSaveDataForDrone(droneId);



        string droneName = _assembly.GetDroneName(droneId);



        GarageDroneData garageDrone = new()
        {
            GarageId = Guid.NewGuid().ToString(),

            DroneId = droneId,

            metaData = new DroneMetadata { Name = droneName},

            CreatedAtTicks = DateTime.UtcNow.Ticks,

            Assembly = assembly
        };

        _garage.Drones.Add(garageDrone);


        Debug.Log($"gggggggg Drone GarageId {garageDrone.GarageId} DroneId {garageDrone.DroneId} DroneName {garageDrone.metaData.Name} CreatedAtTicks {garageDrone.CreatedAtTicks} ");

        Save();
    }



    /// <summary>
    /// ???????? ???????????? ???? ? ??????
    /// ????? ??????????????
    /// </summary>
    public void OverwriteDrone(
        string garageId,
        string droneId)
    {
        GarageDroneData existing =
            Get(garageId);

        if (existing == null)
        {
            Debug.LogError(
                $"Garage drone not found {garageId}");

            return;
        }

        AssemblySaveData assembly =
            _assembly.BuildSaveDataForDrone(droneId);

        string droneName = _assembly.GetDroneName(droneId);

        existing.DroneId = droneId;

        existing.metaData.Name = droneName;

        existing.Assembly = assembly;

        Save();
    }



    public void Delete(string garageId)
    {
        GarageDroneData existing =
            Get(garageId);

        if (existing == null)
            return;

        _garage.Drones.Remove(existing);

        Save();
    }



    public bool Contains(string garageId)
    {
        return _garage.Drones
            .Any(x => x.GarageId == garageId);
    }



    private void Save()
    {
        _saveService.Save(_garage);
    }

    public void Clear()
    {
        _garage.Drones.Clear();
        Save();
    }
}