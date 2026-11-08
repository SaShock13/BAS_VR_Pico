using System;

public sealed class MissionBriefingEvent : IAppEvent
{
    public string EventId => nameof(MissionBriefingEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Briefing { get; }
    public float Duration { get; }

    public MissionBriefingEvent(string briefing, float duration = 0f)
    {
        Briefing = briefing;
        Duration = duration;
    }
}
