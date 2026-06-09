using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/WaitStep")]
public class WaitStepData : MissionStepData
{
    public float WaitSeconds = 3f;


    public override MissionStep CreateStep(SceneMissionBinder binder)
    {
        return new WaitStep(WaitSeconds);
    }
}