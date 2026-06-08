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


    private float _totalCurrent;  // Общее потребление тока

    private BatteryPhysicsData _battery;

    [SerializeField] private bool isVoltageAffects = false ; // Учитывать ли текущий напряжение для рассчета тяги моторов

    public void Initialize(
        DronePhysicsData physicsData
        ,IReadOnlyList<DronePartView> motorViews
        )
    {
        _motors.Clear();

        ApplyRigidbodyData(physicsData);



        _battery = physicsData.Battery;


        Debug.Log($"777777777777Init_battery {_battery!= null}");

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

        SimulateBattery();

    }

    private void SimulateBattery()
    {
        _totalCurrent =
            CalculateCurrentDraw();

        _battery.CurrentDraw =
            _totalCurrent;

        float consumedMah =
            _totalCurrent *
            Time.fixedDeltaTime /
            3600f *
            1000f;

        _battery.CurrentChargeMah -=
            consumedMah;

        _battery.CurrentChargeMah =
            Mathf.Max(
                0,
                _battery.CurrentChargeMah);

        float chargePercent =
            _battery.CurrentChargeMah /
            _battery.CapacityMah;

        float batteryVoltage =
            Mathf.Lerp(
                _battery.EmptyVoltage,
                _battery.FullVoltage,
                chargePercent);

        float sag =
            _totalCurrent *
            _battery.InternalResistance;

        _battery.ActualVoltage =
            Mathf.Max(
                0,
                batteryVoltage - sag);


        Debug.Log($"bbbbbbb   CurrentDraw {_battery.CurrentDraw} _battery.CurrentChargeMa {_battery.CurrentChargeMah} chargePercent {chargePercent} batteryVoltage {batteryVoltage} sag {sag} _battery.ActualVoltage {_battery.ActualVoltage}  ");
    }

    private float CalculateCurrentDraw()
    {
        float total = 0f;

        foreach (DroneMotorRuntime motor in _motors)
        {
            Debug.Log(
            $"bbbbbbbbMotorCurrent={motor.CurrentDraw}");
            total += motor.CurrentDraw;  
        }
        Debug.Log(
        $"bbbbbbbbbbTOTAL CURRENT={total}");
        return total;
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

        Debug.Log(
                    $"bbbbbbbTarget={motor.TargetThrottle} Current={motor.CurrentThrottle}");
        Debug.Log(
    $"bbbbbbbMotorData={motor.Data != null}");

        Debug.Log(
        $"bbbbbbbbIdle={motor.Data.IdleCurrent} " +
        $"Max={motor.Data.MaxCurrent}");


        motor.CurrentDraw =          // todo Сделать зависимость от CurrentThrust
        Mathf.Lerp(
            motor.Data.IdleCurrent,
            motor.Data.MaxCurrent,
            motor.CurrentThrottle);


        Debug.Log(
            $"bbbbbbbbThrottle={motor.CurrentThrottle} Current={motor.CurrentDraw}");


        float voltageFactor = isVoltageAffects ? _battery.ActualVoltage / _battery.NominalBatteryVoltage : 1; // влияние  вольтажа на тягу 

        Debug.Log(
            $"bbbbbbbbvoltageFactor={voltageFactor} ");

        motor.CurrentThrust =
            motor.Data.MaxThrust *
            motor.CurrentThrottle *
            voltageFactor;




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