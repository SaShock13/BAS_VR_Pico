using UnityEngine;

public abstract class MissionStep
{
    protected MissionContext Context;
    public abstract string Description { get; }


    public bool IsCompleted { get; private set; }

    public void Initialize(MissionContext context)
    {
        Context = context;
    }

    public virtual void Enter()
    {
        Debug.Log($"Mission Step Started: {Description}");
       // Debug.Log($"mmmmmmmmEnter step {this}");
    }

    public virtual void Tick()
    {
    }

    public virtual void Exit()
    {
        Debug.Log($"Mission Step Completed: {Description}");
        //Debug.Log($"mmmmmmmmmExit step {this}");
    }

    protected void Complete()
    {
        IsCompleted = true;
    }
}