using UnityEngine;

public abstract class MissionStep
{
    protected MissionContext Context;
    public abstract string Description { get; }

    public virtual Transform Target => null;

    public bool IsCompleted { get; private set; }

    public void Initialize(MissionContext context)
    {
        Context = context;
    }

    public virtual void Enter()
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void Exit()
    {
    }

    protected void Complete()
    {
        IsCompleted = true;
    }
}