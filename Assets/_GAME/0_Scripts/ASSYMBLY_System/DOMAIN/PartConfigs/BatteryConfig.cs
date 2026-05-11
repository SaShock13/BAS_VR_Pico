using UnityEngine;

[CreateAssetMenu]
public class BatteryConfig : PartConfig
{
    [Header("Battery")]

    public float CapacityMah;

    public float Voltage;

    public float MaxDischargeCurrent;
}