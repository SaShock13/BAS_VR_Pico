using System.Collections.Generic;
using UnityEngine;

public class DronePhysicsSimulation : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private float yawTorqueMultiplier = 0.01f;
    private readonly List<DroneMotorRuntime> _motors
        = new();

    public IReadOnlyList<DroneMotorRuntime> Motors
    => _motors;


    public void Initialize(
        DronePhysicsData physicsData
        ,IReadOnlyList<DronePartView> motorViews
        )
    {

        Debug.Log($"Start  Initialize DronePhysicsSimulation{this}");
        _motors.Clear();

        ApplyRigidbodyData(physicsData);

        foreach (MotorPhysicsData motorData
                 in physicsData.Motors)
        {


            Debug.Log($"4444444Пытаюсь получить вью мотора {motorData.InstanceId}");  /// todo Не находит вью . ПОчему???
            DronePartView view =
                FindMotorView(
                    motorData.InstanceId,
                    motorViews);

            if (view == null)
            {
                Debug.LogError(
                    $"MotorView not found: {motorData.InstanceId}");

                continue;
            }

            DroneMotorRuntime runtime =
                new DroneMotorRuntime
                {
                    Data = motorData,
                    Transform = view.transform
                };

            _motors.Add(runtime);

        }
            foreach (var motor in _motors)
            {

                Debug.Log($"Added motor  {motor.Data.InstanceId}");
            }
    }

    private void FixedUpdate()
    {
        SimulateMotors();
    }

    private void SimulateMotors()
    {
        foreach (DroneMotorRuntime motor in _motors)
        {
            SimulateMotor(motor);
            ApplyYawTorque(motor);
        }
    }

    private void SimulateMotor(
     DroneMotorRuntime motor)
    {
        motor.CurrentThrottle =
            Mathf.Lerp(
                motor.CurrentThrottle,
                motor.TargetThrottle,
                motor.Data.ResponseSpeed *
                Time.fixedDeltaTime);

        motor.CurrentThrust =
            motor.Data.MaxThrust *
            motor.CurrentThrottle;

        Vector3 force =
            motor.Transform.up *
            motor.CurrentThrust;


        //Debug.Log($"******* SimulateMotor motorforce {force}");

        _rigidbody.AddForceAtPosition(
            force,
            motor.Transform.position,
            ForceMode.Force);
    }
    private void ApplyYawTorque(
    DroneMotorRuntime motor)
    {
        float yawTorque =
            motor.CurrentThrust *
            motor.Data.MixData.YawFactor *
            yawTorqueMultiplier;


        //Debug.Log($"YawFactor  {motor.Data.MixData.YawFactor}  yawTorque {yawTorque}");
        _rigidbody.AddTorque(
            _rigidbody.transform.up * yawTorque,
            ForceMode.Force);
    }


    private void ApplyRigidbodyData(
        DronePhysicsData data)
    {
        _rigidbody.mass =
            data.TotalMass;

        _rigidbody.centerOfMass =
            data.LocalCenterOfMass;

        _rigidbody.automaticInertiaTensor = true;
    }

    private DronePartView FindMotorView(
        string instanceId,
        IReadOnlyList<DronePartView> views)
    {
        foreach (DronePartView view in views)
        {

            Debug.Log($"4444444Смотрю вью  {view.InstanceId}");
            if (view.InstanceId == instanceId)
                return view;
        }

        return null;
    }

}