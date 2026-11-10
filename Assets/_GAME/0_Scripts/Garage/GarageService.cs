using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GarageService : IGarageService
{
    private const int MaxSlots = 5;

    private readonly Clean_AssemblySystem _assembly;
    private readonly IGarageSaveService _saveService;
    private readonly JsonSaveService _projects;
    private GarageSaveData _garage;
    private string _projectId;

    public GarageService(Clean_AssemblySystem assembly, IGarageSaveService saveService,
        [Zenject.InjectOptional] JsonSaveService projects = null)
    {
        _assembly = assembly;
        _saveService = saveService;
        _projects = projects;
    }

    private GarageSaveData CurrentGarage
    {
        get
        {
            var projectId = AssemblyProjectSession.ActiveId;
            if (_garage != null && _projectId == projectId) return _garage;

            var garage = _saveService.Load() ?? new GarageSaveData();
            foreach (var drone in garage.Drones)
            {
                var parts = drone.Assembly?.Parts;
                if (parts == null) continue;
                var root = parts.FirstOrDefault(p => p.Type == DroneAssemblyRules.RootPartType && string.IsNullOrEmpty(p.AttachedPartId));
                if (root == null)
                {
                    root = parts.FirstOrDefault(p => p.Type == DroneAssemblyRules.RootPartType &&
                        parts.Any(parent => parent.InstanceId == p.AttachedPartId &&
                            parent.Type == PartType.Body && string.IsNullOrEmpty(parent.AttachedPartId)));
                }
                if (root == null) continue;
                drone.DroneId = root.InstanceId;
                if (drone.metaData != null) drone.metaData.DroneId = root.InstanceId;
            }

            _garage = garage;
            _projectId = projectId;
            return garage;
        }
    }

    public bool HasFreeSlot() => CurrentGarage.Drones.Count < MaxSlots;

    public IReadOnlyList<GarageDroneData> GetAll() => CurrentGarage.Drones;

    public GarageDroneData Get(string garageId) =>
        CurrentGarage.Drones.FirstOrDefault(drone => drone.GarageId == garageId);

    public GarageDroneData GetByDroneId(string droneId) =>
        CurrentGarage.Drones.FirstOrDefault(drone => drone.DroneId == droneId ||
            (_assembly.TryGetDroneForPart(drone.DroneId, out var loadedDrone) && loadedDrone.InstanceId == droneId));

    public void SaveDrone(string droneId)
    {
        var drones = new List<GarageDroneData>(CurrentGarage.Drones)
        {
            BuildEntry(Guid.NewGuid().ToString(), droneId, DateTime.UtcNow.Ticks, Color.white)
        };
        Save(new GarageSaveData { Drones = drones });
    }

    public void OverwriteDrone(string garageId, string droneId)
    {
        var drones = new List<GarageDroneData>(CurrentGarage.Drones);
        int index = drones.FindIndex(drone => drone.GarageId == garageId);
        if (index < 0)
        {
            Debug.LogError($"Garage drone not found {garageId}");
            return;
        }

        var existing = drones[index];
        drones[index] = BuildEntry(garageId, droneId, existing.CreatedAtTicks, existing.metaData?.Color ?? Color.white);
        Save(new GarageSaveData { Drones = drones });
    }

    private GarageDroneData BuildEntry(string garageId, string droneId, long createdAtTicks, Color color) =>
        new()
        {
            GarageId = garageId,
            DroneId = droneId,
            metaData = new DroneMetadata { DroneId = droneId, Name = _assembly.GetDroneName(droneId), Color = color },
            CreatedAtTicks = createdAtTicks,
            Assembly = _assembly.BuildSaveDataForDrone(droneId)
        };

    public void Delete(string garageId)
    {
        var drones = new List<GarageDroneData>(CurrentGarage.Drones);
        int index = drones.FindIndex(drone => drone.GarageId == garageId);
        if (index < 0) return;
        drones.RemoveAt(index);
        Save(new GarageSaveData { Drones = drones });
    }

    public bool Contains(string garageId) => Get(garageId) != null;

    public bool MoveToAssembly(string garageId)
    {
        if (_projects == null || !_projects.MoveGarageDroneToAssembly(garageId)) return false;
        _garage = null;
        return true;
    }

    public void Clear()
    {
        if (CurrentGarage.Drones.Count > 0)
            Save(new GarageSaveData());
    }

    private void Save(GarageSaveData garage)
    {
        _saveService.Save(garage);
        _garage = garage;
        _projectId = AssemblyProjectSession.ActiveId;
    }
}
