using UnityEngine;

[CreateAssetMenu]
public class MotorConfig : PartConfig
{
    [Header("Engine")]

    public float MaxThrust;

    public float ResponseSpeed;

    public RotationDirection RotationDirection;

    public float MaxRPM;

    public MotorMixData MixData;
}