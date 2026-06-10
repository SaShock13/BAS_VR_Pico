using System;

public interface ITabletService
{
    event Action<TabletMissionState> MissionChanged;

    TabletMissionState CurrentMissionInfo { get; }

    void SetMission(MissionObjectiveInfo info);

    void ClearMission();
}