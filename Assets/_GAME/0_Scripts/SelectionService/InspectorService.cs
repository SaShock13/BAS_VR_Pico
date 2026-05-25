using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class InspectorService
{
    private readonly ISelectionService _selection;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IPartConfigRegistry _partConfigs;

    public event Action<InspectionContext> Updated;
    public event Action Cleared;

    public InspectorService(ISelectionService selection, Clean_AssemblySystem assembly, IPartConfigRegistry partConfigs )
    {
        _selection = selection;
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

        DroneDomainState drone = null;

        if (!string.IsNullOrEmpty(part.DroneId))
        {
            drone =
                _assembly.GetDroneDomainState(
                    part.DroneId);
        }

        var config = _partConfigs.Get(part.PartId);

        var context = new InspectionContext
        {
            Part = MapPart(part,config),
            Drone = drone != null
                ? MapDrone(drone, config)
                : null,

            IsRootPart =
                part.Type == PartType.Body
        };

        Updated?.Invoke(context);
    }

    private PartViewModel MapPart(PartDomainState domain , PartConfig config)
    {
        return new PartViewModel
        {
            Id = domain.InstanceId,
            Color = domain.VisualProperties.Color,
            Material = domain.VisualProperties.MaterialAddress,
            Weight = config.Mass

        };
    }

    private DroneViewModel MapDrone(DroneDomainState domain, PartConfig config)
    {
        return new DroneViewModel
        {
            Id = domain.InstanceId,
            Name = domain.Name,
            //MotorCount = domain.MotorCount,
            TotalWeight = domain.TotalMass
        };
    }
}