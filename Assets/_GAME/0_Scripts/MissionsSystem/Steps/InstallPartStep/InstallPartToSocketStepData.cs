using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Install Part To Socket")]
public class InstallPartToSocketStepData : MissionStepData
{
    [TextArea(2, 5)]
    public string Description = "Установите деталь в подходящее крепление.";

    public PartType RequiredPartType;
    public PartType RequiredSocketType;

    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        return new InstallPartToSocketStep(
            RequiredPartType,
            RequiredSocketType,
            binder._AssemblySystem,
            binder.EventBus,
            binder._hintScenario,
            Description);
    }
}
