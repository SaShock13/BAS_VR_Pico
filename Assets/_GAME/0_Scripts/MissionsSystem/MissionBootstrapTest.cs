using UnityEngine;

public class MissionBootstrapTest : MonoBehaviour
{
    [SerializeField] private MissionController controller;

    [SerializeField] private MissionDefinition mission;

    [SerializeField] private SceneMissionBinder binder;

    MissionRuntime runtime;
    

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



    private void StartTestMission()
    {
        runtime =
            MissionFactory.CreateRuntime(
                mission,
                binder);

        controller.StartMission(runtime);
    }

}
