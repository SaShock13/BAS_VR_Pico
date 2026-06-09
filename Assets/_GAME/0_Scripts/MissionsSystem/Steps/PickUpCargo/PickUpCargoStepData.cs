using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Pick Up Cargo")]
public class PickUpCargoStepData : MissionStepData
{
    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        return new PickUpCargoStep();
    }
}