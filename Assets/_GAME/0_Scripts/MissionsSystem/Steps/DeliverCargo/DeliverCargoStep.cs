public class DeliverCargoStep : MissionStep
{
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