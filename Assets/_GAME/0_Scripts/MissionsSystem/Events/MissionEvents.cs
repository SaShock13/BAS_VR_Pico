using System;

public static class MissionEvents
{
    public static Action MissionStarted;
    public static Action MissionCompleted;
    public static Action<string> MissionFailed;

    public static Action CargoPickedUp;
    public static Action CargoDelivered;
}