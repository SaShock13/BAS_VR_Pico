using UnityEngine;

public class DeliverCargoStep : MissionStep
{
    private readonly Cargo _cargo;
    public override Transform Target { get; }

    public DeliverCargoStep() { }

    public DeliverCargoStep(Cargo cargo, Transform target)
    {
        _cargo = cargo;
        Target = target;
    }

    public override void Tick()
    {
        if (_cargo != null && Target != null &&
            (Context.Player.position - Target.position).sqrMagnitude <= 2.25f)
            _cargo.Deliver();
    }

    public override string Description => "Deliver Cargo";

    public override void Enter()
    {
        base.Enter();
        MissionEvents.CargoDelivered += OnCargoDelivered;
    }

    public override void Exit()
    {
        base.Exit();
        MissionEvents.CargoDelivered -= OnCargoDelivered;
    }

    private void OnCargoDelivered()
    {
        Complete();
    }
}