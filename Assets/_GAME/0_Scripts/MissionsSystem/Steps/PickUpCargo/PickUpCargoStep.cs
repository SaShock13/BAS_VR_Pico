public class PickUpCargoStep : MissionStep
{
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