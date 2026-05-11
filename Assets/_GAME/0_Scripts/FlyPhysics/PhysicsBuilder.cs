using System.Collections.Generic;
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

        List<MotorPhysicsData> motors = new();

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
                            motorConfig.RotationDirection
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

        //-----------------------------------
        // BUILD RESULT
        //-----------------------------------

        physicsData.TotalMass = totalMass;

        physicsData.CenterOfMass = centerOfMass;

        physicsData.Motors = motors;

        physicsData.Battery = batteryData;

        physicsData.MaxAvailableThrust =
            totalMaxThrust;

        physicsData.HoverThrottle =
            hoverThrottle;

        return physicsData;
    }
}