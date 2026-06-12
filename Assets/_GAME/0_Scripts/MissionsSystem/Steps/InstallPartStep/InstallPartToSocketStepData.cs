using System.Net.Sockets;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Install Part To Socket")]
public class InstallPartToSocketStepData : MissionStepData
{
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
            binder._hintScenario);
    }
}