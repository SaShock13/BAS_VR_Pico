using System;
using UnityEngine;

[Serializable]
public sealed class MotorPhysicsData
{
    public string InstanceId;

    public Vector3 LocalPosition;

    public Vector3 LocalDirection; /// стоит ли использовать LocalRotation вместо?? 

    public Quaternion LocalRotation;

    public float MaxThrust;

    public float ResponseSpeed;

    public RotationDirection RotationDirection; 

    public float MaxRPM;

    public MotorMixData MixData;
}