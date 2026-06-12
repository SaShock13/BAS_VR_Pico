using UnityEngine;

public class InstallPartToSocketStep : MissionStep
{
    private readonly PartType _requiredPartType;
    private readonly PartType _requiredSocketType;

    private readonly Clean_AssemblySystem _assemblySystem;
    private IEventBus _eventBus;

    private readonly IHintScenarioController _hintScenario;

    public override string Description =>
        $"Install {_requiredPartType} into {_requiredSocketType}";


    public InstallPartToSocketStep(
        PartType requiredPartType,
        PartType requiredSocketType,
        Clean_AssemblySystem assemblySystem,
        IEventBus eventBus,
        IHintScenarioController hintScenario
        )
    {
        _requiredPartType = requiredPartType;
        _requiredSocketType = requiredSocketType;
        _assemblySystem = assemblySystem;
        _eventBus = eventBus;
        _hintScenario = hintScenario;
    }

    public override void Enter()
    {

        Debug.Log($"eeeeeeeeeeee_eventBus {_eventBus != null}");
        _eventBus.Subscribe<PartSocketAttachedEvent>(OnPartAttached);
        _hintScenario.StartScenario(
           new HintContext(
               $"Install {_requiredPartType} into {_requiredSocketType}", null, _requiredPartType, _requiredSocketType));
    }

    public override void Exit()
    {
        //_eventBus.<PartSocketAttachedEvent>(OnPartAttached);

        _hintScenario.StopScenario();
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