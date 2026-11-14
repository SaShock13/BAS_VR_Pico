using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Pick Up Cargo")]
public class PickUpCargoStepData : MissionStepData
{
    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        if (binder.Cargo == null)
            return new PickUpCargoStep();
        return new PickUpCargoStep(binder.Cargo, binder.GetPoint(MissionPointId.Pickup).transform);
    }
}