using System;

public sealed class TabletService : ITabletService
{
    public event Action<TabletMissionState> MissionChanged;

    public TabletMissionState CurrentMissionInfo { get; private set; }

    public void SetMission(MissionObjectiveInfo info)
    {
        CurrentMissionInfo = new TabletMissionState   // todo Это лишний слой абстракции - на данный момент вообще не нужен
        {
            MissionName = info.MissionName,
            CurrentStepDefinition = info.Objective,
            CurrentStepIndex = info.CurrentStep,
            TotalSteps = info.TotalSteps
        };

        MissionChanged?.Invoke(CurrentMissionInfo);
    }

    public void ClearMission()
    {
        CurrentMissionInfo = null;

        MissionChanged?.Invoke(null);
    }
}