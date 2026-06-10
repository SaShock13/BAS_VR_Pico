using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Steps/Fly To Point")]
public class FlyToPointStepData : MissionStepData
{
    public MissionPointId PointId; /// todo типобезопасность хорошо бы сделать

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