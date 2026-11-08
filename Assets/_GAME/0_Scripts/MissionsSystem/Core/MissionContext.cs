using UnityEngine;

public class MissionContext
{
    public Transform Player;

    public MissionController Controller;

    public float ElapsedMissionTime;
    public System.Action CompletionNotificationDismissed;
    public System.Action CompletionNotificationCancelled;
}
