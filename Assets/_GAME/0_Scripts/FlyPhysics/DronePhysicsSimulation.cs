using System.Collections.Generic;
using UnityEngine;

public class DronePhysicsSimulation : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private float _maxAngularVelocity = 3;

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
        _motors.Clear();

        ApplyRigidbodyData(physicsData);

        foreach (MotorPhysicsData motorData
                 in physicsData.Motors)
        {
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

    /// <summary>
    /// Симуляция Тяги мотора
    /// </summary>
    /// <param name="motor"></param>
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

        _rigidbody.AddForceAtPosition(
            force,
            motor.Transform.position,
            ForceMode.Force);
    }


    /// <summary>
    /// Применить силу вращения от мотора к дрону
    /// </summary>
    /// <param name="motor"></param>
    private void ApplyYawTorque(
    DroneMotorRuntime motor)
    {
        //float yawTorque =
        //    motor.CurrentThrust *
        //    motor.Data.MixData.YawFactor *
        //    yawTorqueMultiplier;

        //_rigidbody.AddTorque(
        //    _rigidbody.transform.up * yawTorque,
        //    ForceMode.Force);



        float direction =
        motor.Data.RotationDirection ==
        RotationDirection.Clockwise
            ? 1f
            : -1f;

        float yawTorque =
            motor.CurrentThrust *
            yawTorqueMultiplier *
            direction;

        _rigidbody.AddRelativeTorque(
            Vector3.up * yawTorque,
            ForceMode.Force);
    }

    /// <summary>
    /// Применить данные дрона к RigidBOdy вью
    /// </summary>
    /// <param name="data"></param>
    private void ApplyRigidbodyData(
        DronePhysicsData data)
    {
        _rigidbody.mass =
            data.TotalMass;

        _rigidbody.centerOfMass =
            data.LocalCenterOfMass;

        _rigidbody.automaticInertiaTensor = true;

        _rigidbody.isKinematic = false;

        _rigidbody.maxAngularVelocity = _maxAngularVelocity;
    }

    private DronePartView FindMotorView(
        string instanceId,
        IReadOnlyList<DronePartView> views)
    {
        foreach (DronePartView view in views)
        {
            if (view.InstanceId == instanceId)
                return view;
        }

        return null;
    }

}