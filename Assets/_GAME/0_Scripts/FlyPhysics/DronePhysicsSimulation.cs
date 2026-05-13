using System.Collections.Generic;
using UnityEngine;

public class DronePhysicsSimulation : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rigidbody;

    private readonly List<DroneMotorRuntime> _motors
        = new();

    [SerializeField] private float _globalThrottle;

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
        }
    }

    private void SimulateMotor(
        DroneMotorRuntime motor)
    {
        float targetThrust =
            motor.Data.MaxThrust *
            _globalThrottle;

        motor.CurrentThrust =
            Mathf.Lerp(
                motor.CurrentThrust,
                targetThrust,
                motor.Data.ResponseSpeed *
                Time.fixedDeltaTime);

        Vector3 force =
            motor.Transform.up *
            motor.CurrentThrust;

        _rigidbody.AddForceAtPosition(
            force,
            motor.Transform.position,
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

    public void SetThrottle(float value)
    {
        _globalThrottle =
            Mathf.Clamp01(value);
    }
}