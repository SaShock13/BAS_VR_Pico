using UnityEngine;

public class InstallPartToSocketStep : MissionStep
{
    private readonly PartType _requiredPartType;
    private readonly PartType _requiredSocketType;

    private readonly Clean_AssemblySystem _assemblySystem;
    private IEventBus _eventBus;
    private System.IDisposable _subscription;

    private readonly IHintScenarioController _hintScenario;
    private readonly string _description;

    public override string Description => _description;


    public InstallPartToSocketStep(
        PartType requiredPartType,
        PartType requiredSocketType,
        Clean_AssemblySystem assemblySystem,
        IEventBus eventBus,
        IHintScenarioController hintScenario,
        string description = "Установите деталь в подходящее крепление."
        )
    {
        _requiredPartType = requiredPartType;
        _requiredSocketType = requiredSocketType;
        _assemblySystem = assemblySystem;
        _eventBus = eventBus;
        _hintScenario = hintScenario;
        _description = string.IsNullOrWhiteSpace(description)
            ? "Установите деталь в подходящее крепление." : description;
    }

    public override void Enter()
    {

        _subscription = _eventBus.Subscribe<PartSocketAttachedEvent>(OnPartAttached);
        _hintScenario.StartScenario(
           new HintContext(
               Description, null, _requiredPartType, _requiredSocketType));
    }

    public override void Exit()
    {
        _subscription?.Dispose();
        _subscription = null;

        _hintScenario.StopScenario();
    }

    private void OnPartAttached(PartSocketAttachedEvent @event)
    {
        //if (e.AttachedSocketId != _requiredSocketId)  // Проверка по конкретному сокету??
        //    return;

        var domain = _assemblySystem.GetPartDomainState(@event.PartInstanceId);       


        if (domain == null)
            return;
        var partType = domain.Type;



        if (partType != _requiredPartType)  //   Проверка по типу детали
            return;

        Complete();
    }
}
