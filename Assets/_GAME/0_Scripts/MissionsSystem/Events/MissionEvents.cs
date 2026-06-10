using System;
using UnityEngine;

public static class MissionEvents
{
    public static Action<string> MissionStarted;
    public static Action<string> MissionCompleted;
    public static Action<string,string> MissionFailed;

    public static Action CargoPickedUp;
    public static Action CargoDelivered;

    public static Action<MissionObjectiveInfo> ObjectiveChanged;

    public static Action<Transform> TargetChanged;

}