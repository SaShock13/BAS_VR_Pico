public class TimeoutCondition : MissionCondition
{
    private readonly float _maxTime;

    public TimeoutCondition(float maxTime)
    {
        _maxTime = maxTime;
    }

    public override bool IsTriggered()
    {
        return Context.ElapsedMissionTime >= _maxTime;
    }

    public override string FailReason =>
        "Время миссии истекло";
}