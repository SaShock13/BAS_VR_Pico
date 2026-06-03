using UnityEngine;
using Zenject;

/// <summary>
/// Класс для рассчета характеристик дрона .. ПОка тестовые показатели выдает   / todo есть DronePhysicsBuilder, считает DronePhysicsData . Использовать или обьединить или заменить!!
/// </summary>
public class DroneStatsCalculator : IDroneStatsCalculator  
{
    [Inject] private DronePhysicsBuilder _physicsBuilder;



    public Vector3 CalculateCenterOfMassOffset(DroneDomainState drone)
    {

        

        return Vector3.one;
    }

    public float CalculateMass(DroneDomainState drone)
    {
        var mass =  _physicsBuilder.cal

        return 99999;
    }

    public float CalculateTotalThrust(DroneDomainState drone)
    {
        return 1;
    }

    public float EstimateFlightTimeMinutes(DroneDomainState drone)
    {
        return 1;
    }

}
