using UnityEngine;

public class InstallPartToSocketStep : MissionStep
{
    private readonly PartType _requiredPartType;
    private readonly string _requiredSocketId;

    private readonly Clean_AssemblySystem _assemblySystem;
    private IEventBus _eventBus;

    public override string Description =>
        $"Install {_requiredPartType} into {_requiredSocketId}";


    public InstallPartToSocketStep(
        PartType requiredPartType,
        string requiredSocketId,
        Clean_AssemblySystem assemblySystem,
        IEventBus eventBus
        )
    {
        _requiredPartType = requiredPartType;
        _requiredSocketId = requiredSocketId;
        _assemblySystem = assemblySystem;
        _eventBus = eventBus;
    }

    public override void Enter()
    {

        Debug.Log($"eeeeeeeeeeee_eventBus {_eventBus != null}");
        _eventBus.Subscribe<PartSocketAttachedEvent>(OnPartAttached);
    }

    public override void Exit()
    {
        //_eventBus.<PartSocketAttachedEvent>(OnPartAttached);
    }

    private void OnPartAttached(PartSocketAttachedEvent @event)
    {
        //if (e.AttachedSocketId != _requiredSocketId)  // Проверка по конкретному сокету??
        //    return;

        var domain = _assemblySystem.GetPartDomainState(@event.PartInstanceId);       


        Debug.Log($"++++domain {domain != null}");
        var partType = domain.Type;

        Debug.Log($"++++partType {partType}");


        if (partType != _requiredPartType)  //   Проверка по типу детали
            return;

        Complete();
    }
}