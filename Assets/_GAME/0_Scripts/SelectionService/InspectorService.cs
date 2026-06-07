using System;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Zenject;

public class InspectorService
{
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IPartConfigRepository _partConfigs;
    [Inject] private readonly IGarageService _garage;
    [Inject] private readonly AddressablesAssetService _assets;

    IEventBus _eventBus;

    public event Action<InspectionContext> Updated;
    public event Action Cleared;

    public InspectorService(ISelectionService selection, Clean_AssemblySystem assembly, IEventBus eventBus, IPartConfigRepository partConfigs )
    {
        _selection = selection;
        _eventBus = eventBus;
        _assembly = assembly;
        _partConfigs = partConfigs;
        _selection.Changed += OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectionTarget? target)
    {
        if (target == null)
        {
            Cleared?.Invoke();
            return;
        }



        PartDomainState part =
            _assembly.GetPartDomainState(
                target.Value.PartId);

        Debug.Log($"ssssssssssSelectTarget  {part.PartId}");
        Debug.Log($"ssssssssssSelectTarget  {part.DroneId}");

        DroneDomainState drone = null;

        if (!string.IsNullOrEmpty(part.DroneId))
        {
            drone =
                _assembly.GetDroneDomainState(
                    part.DroneId);
        }

        var garageDrone = _garage.GetByDroneId(part.DroneId);

        var config = _partConfigs.Get(part.PartId);


        Debug.Log($"xxxxxxxxxxSlected Drone name {drone}");

        string droneName = "";

        if (drone != null)
        {
            droneName =
                garageDrone?.metaData.Name
                ?? _assembly.GetDroneName(drone.InstanceId);
        }

        var context = new InspectionContext
        {
            Part = MapPart(part,config), 
            Drone = drone != null    /// todo Имя брать из garageDrone
                ? MapDrone(drone, droneName) 
                : null,

            IsRootPart =
                part.Type == PartType.Body
        };


        _eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now }); // todo дорабьотать! Момент когда сохраняет снапшот сейчас дублируется
        Updated?.Invoke(context);
    }

    private PartViewModel MapPart(PartDomainState domain , PartConfig config)
    {
        return new PartViewModel
        {
            InstanceId = domain.InstanceId,
            Name = config.PartId,
            Color = domain.VisualProperties.Color,
            Material = domain.VisualProperties.MaterialAddress,
            Weight = config.Mass

        };
    }

    private DroneViewModel MapDrone(DroneDomainState domain, string name)  
    {
        var droneName = _assembly.GetDroneName(domain.InstanceId);

        Debug.Log($"zzzzzzzzzzdroneName from Ass {droneName}");
        var weight = _assembly.GetComputed(domain.InstanceId).TotalMass;
        return new DroneViewModel
        {
            Id = domain.InstanceId,
            Name = name,
            //MotorCount = domain.MotorCount,
            TotalWeight = weight
        };
    }
}