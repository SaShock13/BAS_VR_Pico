using System;

public sealed class CreatePartStep : MissionStep
{
    private readonly PartType _type;
    private readonly string _description;
    private readonly Clean_AssemblySystem _assembly;
    private readonly IEventBus _events;
    private readonly IHintScenarioController _hints;
    private IDisposable _created;
    private IDisposable _changed;
    private IDisposable _rebuilt;
    private bool _needsCheck;

    public override string Description => _description;

    public CreatePartStep(PartType type, string description, Clean_AssemblySystem assembly,
        IEventBus events, IHintScenarioController hints)
    {
        _type = type;
        _description = description;
        _assembly = assembly;
        _events = events;
        _hints = hints;
    }

    public override void Enter()
    {
        _created = _events.Subscribe<Clean_PartCreatedEvent>(_ => _needsCheck = true);
        _changed = _events.Subscribe<AssemblyChangedEvent>(_ => _needsCheck = true);
        _rebuilt = _events.Subscribe<DronesRebuiltEvent>(_ => _needsCheck = true);
        _needsCheck = true;
        Tick();
        if (!IsCompleted)
            _hints.StartScenario(new HintContext(Description, null));
    }

    public override void Tick()
    {
        if (!_needsCheck)
            return;
        _needsCheck = false;
        if (_assembly.HasPartOfType(_type))
            Complete();
    }

    public override void Exit()
    {
        _created?.Dispose();
        _changed?.Dispose();
        _rebuilt?.Dispose();
        _created = _changed = null;
        _rebuilt = null;
        _hints.StopScenario();
    }
}
