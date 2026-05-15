using System;
using UnityEngine;

[Serializable]
public class MotorMixData
{
    [Range(-1f, 1f)]
    public float PitchFactor;

    [Range(-1f, 1f)]
    public float RollFactor;

    [Range(-1f, 1f)]
    public float YawFactor;
}