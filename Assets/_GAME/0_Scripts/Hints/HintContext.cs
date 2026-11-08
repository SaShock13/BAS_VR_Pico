using System.Net.Sockets;
using UnityEngine;

public readonly struct HintContext
{
    public readonly string HintText;


    public readonly Transform Target;
    public readonly bool PointAtTarget;

    public readonly PartType? RequiredPartType;
    public readonly PartType? RequiredSocketType;

    public HintContext(
        string hintText,
        Transform target,
        PartType? reqPartType = null,
        PartType? reqSocketType = null,
        bool pointAtTarget = false
        )
    {
        HintText = hintText;
        Target = target;
        RequiredPartType = reqPartType;
        RequiredSocketType = reqSocketType;
        PointAtTarget = pointAtTarget;
    }
}
