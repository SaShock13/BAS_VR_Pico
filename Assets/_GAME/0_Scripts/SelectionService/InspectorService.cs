using System;
using UnityEngine;
using Zenject;

public class InspectorService
{
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IPartConfigRepository _partConfigs;
    [Inject] private readonly IGarageService _garage;
    [Inject] private readonly AddressablesAssetService _assets;
    [Inject] private readonly IMaterialRegistry _materials;

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
        _eventBus.Subscribe<PartVisualChangedEvent>(OnPartChanged);
    }

    private void OnPartChanged(PartVisualChangedEvent @event)
    {
        var selected = _selection.Current;

        if (selected == null)
            return;

        if (selected.Value.PartId != @event.InstanceId)
            return;

        RefreshSelectedPart();
    }

    private void RefreshSelectedPart()
    {
        var target = _selection.Current;

        if (target == null)
        {
            Cleared?.Invoke();
            return;
        }

        PartDomainState part =
            _assembly.GetPartDomainState(
                target.Value.PartId);

        DroneDomainState drone = null;

        if (!string.IsNullOrEmpty(part.DroneId))
        {
            drone =
                _assembly.GetDroneDomainState(
                    part.DroneId);
        }

        var garageDrone =
            _garage.GetByDroneId(part.DroneId);

        var config =
            _partConfigs.Get(part.PartId);

        string droneName = "";

        if (drone != null)
        {
            droneName =
                garageDrone?.metaData.Name
                ?? _assembly.GetDroneName(drone.InstanceId);
        }

        var context = new InspectionContext
        {
            Part = MapPart(part, config),

            Drone = drone != null
                ? MapDrone(drone, droneName)
                : null,

            IsRootPart =
                part.Type == PartType.Body
        };

        Updated?.Invoke(context);
    }


    private void OnSelectionChanged(SelectionTarget? target)
    {

        RefreshSelectedPart();

        //if (target == null)
        //{
        //    Cleared?.Invoke();
        //    return;
        //}



        //PartDomainState part =
        //    _assembly.GetPartDomainState(
        //        target.Value.PartId);

        //Debug.Log($"ssssssssssSelectTarget  {part.PartId}");
        //Debug.Log($"ssssssssssSelectTarget  {part.DroneId}");

        //DroneDomainState drone = null;

        //if (!string.IsNullOrEmpty(part.DroneId))
        //{
        //    drone =
        //        _assembly.GetDroneDomainState(
        //            part.DroneId);
        //}

        //var garageDrone = _garage.GetByDroneId(part.DroneId);

        //var config = _partConfigs.Get(part.PartId);


        //Debug.Log($"xxxxxxxxxxSlected Drone name {drone}");

        //string droneName = "";

        //if (drone != null)
        //{
        //    droneName =
        //        garageDrone?.metaData.Name
        //        ?? _assembly.GetDroneName(drone.InstanceId);
        //}

        //var context = new InspectionContext
        //{
        //    Part = MapPart(part,config), 
        //    Drone = drone != null    /// todo Имя брать из garageDrone
        //        ? MapDrone(drone, droneName) 
        //        : null,

        //    IsRootPart =
        //        part.Type == PartType.Body
        //};


        //_eventBus.Publish(new AssemblyChangedEvent { Timestamp = DateTime.Now }); // todo дорабьотать! Момент когда сохраняет снапшот сейчас дублируется
        //Updated?.Invoke(context);
    }




    private PartViewModel MapPart(PartDomainState domain , PartConfig config)
    {
        var materialDefinition = _materials.Get(domain.VisualProperties.MaterialId);

        return new PartViewModel
        {
            InstanceId = domain.InstanceId,
            Name = config.PartId,
            Color = domain.VisualProperties.Color,
            Material = materialDefinition.DisplayName,
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