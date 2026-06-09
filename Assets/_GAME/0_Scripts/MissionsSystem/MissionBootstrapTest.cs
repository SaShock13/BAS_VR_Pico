using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionBootstrapTest : MonoBehaviour
{
    [SerializeField] private MissionController controller;

    [SerializeField] private MissionDefinition mission;

    [SerializeField] private SceneMissionBinder binder;

    MissionRuntime runtime;

    private void OnEnable()
    {
        MissionEvents.MissionStarted += OnMissionStarted;
        MissionEvents.MissionCompleted += OnMissionCompleted;
        MissionEvents.MissionFailed += OnMissionFailed;
    }

    private void OnMissionFailed(string obj)
    {

        Debug.Log($"mmmmmmmmmmOnMissionFailed {this}");
    }

    private void OnMissionCompleted()
    {
        Debug.Log($"mmmmmmmmmmOnMissionCompleted {this}");
    }

    private void OnMissionStarted()
    {
        Debug.Log($"mmmmmmmmmmOnMissionStarted {this}");
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartTestMission();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            MissionEvents.CargoPickedUp?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            MissionEvents.CargoDelivered?.Invoke();
        }
    }



    private void Start()
    {
        
    }


    private void StartTestMission()
    {
        //var steps = new List<MissionStep>
        //{
        //    new WaitStep(3),
        //    new WaitStep(5)
        //};

        //var failConditions = new List<MissionCondition>
        //{
        //    new TimeoutCondition(20)
        //};

        //runtime = new MissionRuntime(
        //    steps,
        //    failConditions);

        //controller.StartMission(runtime);


        runtime =
            MissionFactory.CreateRuntime(
                mission,
                binder);

        controller.StartMission(runtime);
    }

}
