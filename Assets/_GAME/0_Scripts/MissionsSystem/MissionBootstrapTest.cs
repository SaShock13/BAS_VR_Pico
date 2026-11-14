using UnityEngine;
using UnityEngine.InputSystem;

public class MissionBootstrapTest : MonoBehaviour
{
    [SerializeField] private MissionController controller;

    [SerializeField] private MissionDefinition mission;

    [SerializeField] private SceneMissionBinder binder;

    [SerializeField] private InputActionReference startMissionAction;

    private InputAction _startAction;
    private bool _enabledAction;

    private void OnEnable()
    {
        _startAction = startMissionAction != null ? startMissionAction.action : null;
        if (_startAction == null)
            return;
        _startAction.performed += OnStartMission;
        _enabledAction = !_startAction.enabled;
        if (_enabledAction)
            _startAction.Enable();
    }

    private void OnDisable()
    {
        if (_startAction == null)
            return;
        _startAction.performed -= OnStartMission;
        if (_enabledAction)
            _startAction.Disable();
        _startAction = null;
    }

    private void OnStartMission(InputAction.CallbackContext context) => StartTestMission();

    public void StartTestMission()
    {
        if (FlightSession.Mission != null)
            return;
        if (controller == null || mission == null || binder == null)
        {
            Debug.LogError("Mission bootstrap references are not assigned.", this);
            return;
        }
        if (controller.State == MissionState.Running || controller.State == MissionState.Paused)
            return;

        var runtime =
            MissionFactory.CreateRuntime(
                mission,
                binder);

        controller.StartMission(runtime);
    }

}
