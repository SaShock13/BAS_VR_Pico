using UnityEngine;

public class WaitStep : MissionStep
{
    private readonly float _seconds;

    private float _timer;

    public override string Description =>
     $"Wait for {_seconds} seconds";

    public WaitStep(float seconds)
    {
        _seconds = seconds;
    }


    public override void Enter()
    {
        base.Enter();
        _timer = 0;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Tick()
    {
        _timer += Time.deltaTime;

        if (_timer >= _seconds)
            Complete();
    }
}