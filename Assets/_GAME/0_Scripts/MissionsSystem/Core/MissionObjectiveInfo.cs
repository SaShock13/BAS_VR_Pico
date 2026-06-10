public readonly struct MissionObjectiveInfo
{
    public string MissionName { get;  }

    public string Objective { get; }

    public int CurrentStep { get; }

    public int TotalSteps { get; }
    public string Briefing { get;}

    public MissionObjectiveInfo(string name, string briefing, string objective, int stepNum , int totalSteps)
    {
        MissionName = name;
        Briefing = briefing;
        Objective = objective;
        CurrentStep = stepNum;
        TotalSteps = totalSteps;
    }
}