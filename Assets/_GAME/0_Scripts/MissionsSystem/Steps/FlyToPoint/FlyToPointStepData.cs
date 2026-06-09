using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Fly To Point")]
public class FlyToPointStepData : MissionStepData
{
    public string PointId;

    public float Radius = 3f;

    public override MissionStep CreateStep(
        SceneMissionBinder binder)
    {
        var point = binder.GetPoint(PointId);

        return new FlyToPointStep(
            point.transform,
            Radius);
    }
}