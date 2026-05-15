using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class DronePhysicsBuilder
{
    private readonly IPartConfigRegistry _configRegistry;
    private readonly PartViewRegistry _viewRegistry;


    //[Inject]   // Синглтон или ?? И может при создании просто прокидывать зависимости без Зенджекта??
    public DronePhysicsBuilder(
        IPartConfigRegistry configRegistry,
        PartViewRegistry viewRegistry)
    {
        _configRegistry = configRegistry;
        _viewRegistry = viewRegistry;
    }

    public DronePhysicsData Build(
        IReadOnlyCollection<PartDomainState> parts,
        Transform droneRoot)
    {
        DronePhysicsData physicsData = new DronePhysicsData();

        List<MotorPhysicsData> motors = new();  // должен иметь фикисрованый порядок , для корректного обхода списка

        float totalMass = 0f;

        Vector3 weightedCenterSum = Vector3.zero;

        BatteryPhysicsData batteryData = null;

        float totalMaxThrust = 0f;

        foreach (PartDomainState part in parts)
        {
            PartConfig config =
                _configRegistry.Get(part.PartId);

            _viewRegistry.TryGet(part.InstanceId,out DronePartView view);

            Transform transform = view.transform;

            //-----------------------------------
            // MASS
            //-----------------------------------

            totalMass += config.Mass;

            //-----------------------------------
            // CENTER OF MASS
            //-----------------------------------

            Vector3 worldCenterOfMass =
                transform.TransformPoint(config.LocalCenterOfMass);

            Vector3 localCenterOfMass =
                droneRoot.InverseTransformPoint(worldCenterOfMass);

            weightedCenterSum +=
                localCenterOfMass * config.Mass;

            //-----------------------------------
            // MOTORS
            //-----------------------------------

            if (config is MotorConfig motorConfig)
            {
                Vector3 localMotorPosition =
                    droneRoot.InverseTransformPoint(
                        transform.position);

                Vector3 localMotorDirection =
                    droneRoot.InverseTransformDirection(
                        transform.up);

                MotorMixData motorMixData =
                    new MotorMixData()   /// todo надо ли заполнять - потом тоже заполняется
                    {

                        YawFactor = motorConfig.RotationDirection == RotationDirection.CounterClockwise ? -1f : 1f,
                        PitchFactor = motorConfig.MixData.PitchFactor,
                        RollFactor = motorConfig.MixData.RollFactor

                    };

                MotorPhysicsData motorData =
                    new MotorPhysicsData()
                    {
                        InstanceId = part.InstanceId,

                        LocalPosition = localMotorPosition,

                        LocalDirection = localMotorDirection,

                        MaxThrust = motorConfig.MaxThrust,

                        ResponseSpeed =
                            motorConfig.ResponseSpeed,

                        RotationDirection =
                            motorConfig.RotationDirection,

                        MixData = motorMixData

                    };

                motors.Add(motorData);

                totalMaxThrust +=
                    motorConfig.MaxThrust;
            }

            //-----------------------------------
            // BATTERY
            //-----------------------------------

            if (config is BatteryConfig batteryConfig)
            {
                batteryData =
                    new BatteryPhysicsData()
                    {
                        CapacityMah =
                            batteryConfig.CapacityMah,

                        CurrentChargeMah =
                            batteryConfig.CapacityMah,

                        Voltage =
                            batteryConfig.Voltage
                    };
            }
        }

        //-----------------------------------
        // FINAL CENTER OF MASS
        //-----------------------------------

        Vector3 centerOfMass =
            Vector3.zero;

        if (totalMass > 0f)
        {
            centerOfMass =
                weightedCenterSum / totalMass;
        }

        //-----------------------------------
        // HOVER THROTTLE
        //-----------------------------------

        float hoverThrottle = 0f;

        if (totalMaxThrust > 0f)
        {
            float gravityForce =
                totalMass * Physics.gravity.magnitude;

            hoverThrottle =
                gravityForce / totalMaxThrust;
        }


        CalculateMotorFactors(motors , centerOfMass);
        var yawAttachmentResult = CalculateYawFactors(motors , centerOfMass);

        //-----------------------------------
        // BUILD RESULT
        //-----------------------------------

        physicsData.TotalMass = totalMass;

        physicsData.LocalCenterOfMass = centerOfMass;

        physicsData.Motors = motors;

        physicsData.Battery = batteryData;

        physicsData.MaxAvailableThrust =
            totalMaxThrust;

        physicsData.HoverThrottle =
            hoverThrottle;

        physicsData.YawBias = yawAttachmentResult.YawBias;

        return physicsData;
    }





    public void CalculateMotorFactors(   /// !todo Рассчет переделать . Нужны трансформы вью и 
        List<MotorPhysicsData> motors,
        Vector3 centerOfMass)
    {
        float maxPitch = 0f;
        float maxRoll = 0f;

        foreach (var motor in motors)
        {
            Vector3 offset =
                motor.LocalPosition -
                centerOfMass;

            maxPitch =
                Mathf.Max(
                    maxPitch,
                    Mathf.Abs(offset.z));

            maxRoll =
                Mathf.Max(
                    maxRoll,
                    Mathf.Abs(offset.x));
        }

        foreach (var motor in motors)
        {
            Vector3 offset =
                motor.LocalPosition -
                centerOfMass;

            float pitch =
                -offset.z / maxPitch;

            float roll =
                offset.x / maxRoll;

            float yaw =
                motor.RotationDirection ==
                RotationDirection.CounterClockwise
                    ? 1f
                    : -1f;

            motor.MixData =
                new MotorMixData
                {
                    PitchFactor = pitch,
                    RollFactor = roll,
                    YawFactor = yaw
                };


            Debug.Log($"555555555555 Motor {motor.InstanceId} pitchFActor {pitch}  rollFActor {roll}");
        }
    }



    /// <summary>
    /// Сортирует моторы, и распределяет направления им вращения , если нечетное количесво - вычисляет фактор смещения кругового
    /// </summary>
    /// <param name="motors"></param>
    /// <param name="centerOfMass"></param>
    /// <returns></returns>
    public RotationAssignmentResult CalculateYawFactors(
        List<MotorPhysicsData> motors,
        Vector3 centerOfMass)
    {
        if (motors == null || motors.Count == 0)
        {
            return new RotationAssignmentResult();
        }

        // 1. Сортируем моторы по углу вокруг COM
        List<MotorAngleData> ordered =
            motors
                .Select(m =>
                {
                    Vector3 offset =
                        m.LocalPosition -
                        centerOfMass;

                    float angle =
                        Mathf.Atan2(
                            offset.x,
                            offset.z);

                    return new MotorAngleData
                    {
                        Motor = m,
                        Angle = angle
                    };
                })
                .OrderBy(x => x.Angle)
                .ToList();

        // 2. Назначаем CW / CCW по кругу
        for (int i = 0; i < ordered.Count; i++)
        {
            RotationDirection dir =
                i % 2 == 0
                    ? RotationDirection.Clockwise
                    : RotationDirection.CounterClockwise;

            ordered[i].Motor.RotationDirection =
                dir;

            ordered[i].Motor.MixData.YawFactor =
                dir == RotationDirection.Clockwise
                    ? -1f
                    : 1f;

            // optional debug index
            //ordered[i].Motor.MotorOrderIndex = i;
        }

        // 3. Проверяем imbalance
        int cwCount =
            ordered.Count(x =>
                x.Motor.RotationDirection ==
                RotationDirection.Clockwise);

        int ccwCount =
            ordered.Count(x =>
                x.Motor.RotationDirection ==
                RotationDirection.CounterClockwise);

        int yawBias =
            ccwCount - cwCount;

        bool hasImbalance =
            yawBias != 0;

        return new RotationAssignmentResult
        {
            HasYawImbalance = hasImbalance,
            YawBias = yawBias,
            OrderedMotors =
                ordered
                    .Select(x => x.Motor)
                    .ToList()
        };
    }

    private class MotorAngleData
    {
        public MotorPhysicsData Motor;

        public float Angle;
    }
    public class RotationAssignmentResult
    {
        public bool HasYawImbalance;

        public int YawBias;

        public List<MotorPhysicsData> OrderedMotors =  /// порядок моторов на всякий случай
            new();
    }

}