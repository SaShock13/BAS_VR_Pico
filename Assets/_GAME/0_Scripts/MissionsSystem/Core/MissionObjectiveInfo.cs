public readonly struct MissionObjectiveInfo
{
    public string MissionName { get;  }

    public string Objective { get; }

    public int CurrentStep { get; }

    public int TotalSteps { get; }
    public string Briefing { get;}
    public float NotificationDuration { get; }

    public MissionObjectiveInfo(string name, string briefing, string objective, int stepNum , int totalSteps,
        float notificationDuration = 0f)
    {
        MissionName = name;
        Briefing = briefing;
        Objective = objective;
        CurrentStep = stepNum;
        TotalSteps = totalSteps;
        NotificationDuration = notificationDuration > 0f ? notificationDuration : 3f;
    }
}
