using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Install Part To Socket")]
public class InstallPartToSocketStepData : MissionStepData
{
    public PartType RequiredPartType;
    public string RequiredSocketId;

    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        return new InstallPartToSocketStep(
            RequiredPartType,
            RequiredSocketId,
            binder._AssemblySystem,
            binder.EventBus);
    }
}