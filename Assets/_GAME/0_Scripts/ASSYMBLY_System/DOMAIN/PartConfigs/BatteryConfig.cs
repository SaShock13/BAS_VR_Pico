using UnityEngine;

[CreateAssetMenu]
public class BatteryConfig : PartConfig
{
    [Header("Battery")]

    public int CellCount;

    public float CapacityMah;

    public float NominalVoltage;

    public float FullVoltage;

    public float EmptyVoltage;

    public float InternalResistance;

    public float MaxDischargeCurrent;
}