using System;
using Pico.Platform;
using UnityEngine;
using Zenject;

public class MissionViewController : MonoBehaviour
{
    [Inject] private ITabletService _tablet;
    [Inject] private INotificationService _notification;

    private void OnEnable()
    {
        MissionEvents.MissionStarted += OnMissionStarted;
        MissionEvents.MissionCompleted += OnMissionCompleted;
        MissionEvents.MissionFailed += OnMissionFailed;
        MissionEvents.ObjectiveChanged += OnObjectiveChanged;
    }


    private void OnDisable()
    {
        MissionEvents.MissionStarted -= OnMissionStarted;
        MissionEvents.MissionCompleted -= OnMissionCompleted;
        MissionEvents.MissionFailed -= OnMissionFailed;
        MissionEvents.ObjectiveChanged -= OnObjectiveChanged;
    }

    private void OnMissionStarted(string name)
    {
        Debug.Log("+++++++++Mission Started");
        _notification.Info($"Миссия {name} начата.");

    }

    private void OnMissionCompleted(string name)
    {
        Debug.Log("+++++++++++Mission Completed");
        _tablet.ClearMission();
        _notification.Info($"Миссия {name} завершена.");
    }

    private void OnMissionFailed(string name, string reason)
    {
        Debug.LogError($"++++++++++++Миссия {name} Провалена : {reason}");
    }

    private void OnObjectiveChanged(MissionObjectiveInfo info)
    {
        Debug.Log($"++++++++++Objective: {info.Objective}  MissionName {info.MissionName}");

        _tablet.SetMission(info);
        _notification.Info(info.Objective);
    }
}
