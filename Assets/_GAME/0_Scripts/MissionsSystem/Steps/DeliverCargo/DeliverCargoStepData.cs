using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Deliver Cargo")]
public class DeliverCargoStepData : MissionStepData
{
    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        if (binder.Cargo == null)
            return new DeliverCargoStep();
        return new DeliverCargoStep(binder.Cargo, binder.GetPoint(MissionPointId.Delivery).transform);
    }
}