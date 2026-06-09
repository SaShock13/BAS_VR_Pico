public abstract class MissionCondition
{
    protected MissionContext Context;

    public void Initialize(MissionContext context)
    {
        Context = context;
    }

    public abstract bool IsTriggered();

    public abstract string FailReason { get; }
}