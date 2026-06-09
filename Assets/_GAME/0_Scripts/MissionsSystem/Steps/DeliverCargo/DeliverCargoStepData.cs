using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Deliver Cargo")]
public class DeliverCargoStepData : MissionStepData
{
    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        return new DeliverCargoStep();
    }
}