using UnityEngine;

[CreateAssetMenu(menuName = "Mission System/Steps/Launch Flight")]
public sealed class LaunchFlightStepData : MissionStepData
{
    [TextArea(2, 4)]
    public string Description = "Найдите панель рядом со столом и нажмите «В полёт».";

    public override MissionStep CreateStep(SceneMissionBinder binder)
    {
        return new LaunchFlightStep(binder.EventBus, binder._hintScenario,
            binder.FlightLaunchTarget, Description);
    }
}
