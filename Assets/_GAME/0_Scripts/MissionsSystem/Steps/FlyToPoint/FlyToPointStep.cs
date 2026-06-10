using UnityEngine;

public class FlyToPointStep : MissionStep
{
    private readonly Transform _target;
    private readonly float _radius;
    public override string Description => $"Fly To {_target.name}";

    public override Transform Target => _target;


    public FlyToPointStep(
        Transform target,
        float radius)
    {
        _target = target;
        _radius = radius;
    }

    

    public override void Tick()
    {
        float distance = Vector3.Distance(
            Context.Player.position,
            _target.position);

        if (distance <= _radius)
        {
            Complete();
        }
    }
}