using UnityEngine;

public class PickUpCargoStep : MissionStep
{
    private readonly Cargo _cargo;
    public override Transform Target { get; }

    public PickUpCargoStep() { }

    public PickUpCargoStep(Cargo cargo, Transform target)
    {
        _cargo = cargo;
        Target = target;
    }

    public override void Tick()
    {
        if (_cargo != null && Target != null &&
            (Context.Player.position - Target.position).sqrMagnitude <= 2.25f)
            _cargo.PickUp();
    }

    public override string Description => "Pick Up Cargo";

    public override void Enter()
    {
        base.Enter();
        MissionEvents.CargoPickedUp += OnCargoPickedUp;
    }

    public override void Exit()
    {
        base.Exit();
        MissionEvents.CargoPickedUp -= OnCargoPickedUp;
    }

    private void OnCargoPickedUp()
    {
        Complete();
    }
}