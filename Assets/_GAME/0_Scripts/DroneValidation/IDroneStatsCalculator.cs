using UnityEngine;

public interface IDroneStatsCalculator
{
    float CalculateMass(
        DroneDomainState drone);

    float CalculateTotalThrust(
        DroneDomainState drone);

    Vector3 CalculateCenterOfMassOffset(
        DroneDomainState drone);

    float EstimateFlightTimeMinutes(
        DroneDomainState drone);
}