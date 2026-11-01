using System;
using UnityEngine;
using Zenject;

public class InspectorService : IDisposable
{
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IPartConfigRepository _partConfigs;
    [Inject] private readonly IGarageService _garage;
    [Inject] private readonly AddressablesAssetService _assets;
    [Inject] private readonly IMaterialRegistry _materials;

    IEventBus _eventBus;
    private readonly IDisposable _visualSubscription;
    private readonly IDisposable _rebuildSubscription;

    public event Action<InspectionContext> Updated;
    public event Action Cleared;

    public InspectorService(ISelectionService selection, Clean_AssemblySystem assembly, IEventBus eventBus, IPartConfigRepository partConfigs )
    {
        _selection = selection;
        _eventBus = eventBus;
        _assembly = assembly;
        _partConfigs = partConfigs;
        _selection.Changed += OnSelectionChanged;
        _visualSubscription = _eventBus.Subscribe<PartVisualChangedEvent>(OnPartChanged);
        _rebuildSubscription = _eventBus.Subscribe<DronesRebuiltEvent>(_ => RefreshSelectedPart());
    }

    public bool TryGetSelectedDrone(out DroneDomainState drone)
    {
        drone = null;
        var target = _selection.Current;
        return target.HasValue && _assembly.TryGetDroneForPart(target.Value.PartId, out drone);
    }

    public void Dispose()
    {
        _selection.Changed -= OnSelectionChanged;
        _visualSubscription.Dispose();
        _rebuildSubscription.Dispose();
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

        if (!_assembly.TryGetPartDomainState(target.Value.PartId, out var part))
        {
            Cleared?.Invoke();
            return;
        }

        TryGetSelectedDrone(out var drone);

        var garageDrone =
            drone != null ? _garage.GetByDroneId(drone.InstanceId) : null;

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
            Smoothness = domain.VisualProperties.Smoothness,
            Metallness = domain.VisualProperties.Metallic,
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
