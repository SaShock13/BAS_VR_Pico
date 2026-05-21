using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class FlightController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    private DroneMixer _mixer;

    [SerializeField]
    private DronePhysicsSimulation _simulation;

    [SerializeField] private DroneDebugInput _input;

    [Header("Rates")]
    [SerializeField] private float _maxPitchRate = 1.5f;
    [SerializeField] private float _maxRollRate = 1.5f;
    [SerializeField] private float _maxYawRate = 1f;

    [Header("Stabilization")]
    [SerializeField] private float _pitchGain = 0.2f;
    [SerializeField] private float _rollGain = 0.2f;
    [SerializeField] private float _yawGain = 0.1f;

    [Header("PID")]
    [SerializeField]
    private PIDController _pitchPID;
    [SerializeField]
    private PIDController _rollPID;
    [SerializeField]
    private PIDController _yawPID;

    private bool _enabled = false;


    [Header("Max Corrections")]
    [SerializeField]
    private float _maxPitchCorrection = 0.2f;
    [SerializeField]
    private float _maxRollCorrection = 0.2f;
    [SerializeField]
    private float _maxYawCorrection = 0.1f;

    private void Awake()
    {
        _mixer = new();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            _simulation = FindAnyObjectByType<DronePhysicsSimulation>();
            _rb = _simulation.GetComponent<Rigidbody>();
            _input = FindAnyObjectByType<DroneDebugInput>();
            if(_input != null && _simulation !=null)  _enabled = true;
        }

    }

    private void FixedUpdate()
    {
        if (!_enabled) return;

        FlightInput input = _input.CurrentInput;

        Vector3 localAngularVelocity =
            transform.InverseTransformDirection(_rb.angularVelocity);

        float desiredPitchRate =
            input.Pitch * _maxPitchRate;

        float desiredRollRate =
            -input.Roll * _maxRollRate;

        float desiredYawRate =
            -input.Yaw * _maxYawRate;

        float pitchError =
            desiredPitchRate - localAngularVelocity.x;

        float rollError =
            desiredRollRate - localAngularVelocity.z;

        float yawError =
            desiredYawRate - localAngularVelocity.y;

        MixerInput mixerInput = new MixerInput
        {
            Throttle = input.Throttle,

            PitchCorrection = _pitchPID.Update( pitchError, Time.fixedDeltaTime),
            RollCorrection = _rollPID.Update(rollError, Time.fixedDeltaTime),
            YawCorrection = _yawPID.Update(yawError, Time.fixedDeltaTime),
            
        };

        mixerInput.PitchCorrection = Mathf.Clamp(
                        mixerInput.PitchCorrection,
                        -_maxPitchCorrection,
                        _maxPitchCorrection);

        mixerInput.RollCorrection = Mathf.Clamp(
                        mixerInput.RollCorrection,
                        -_maxRollCorrection,
                        _maxRollCorrection);

        mixerInput.YawCorrection = Mathf.Clamp(
                                        mixerInput.YawCorrection  ,
                                        -_maxYawCorrection,
                                        _maxYawCorrection);

        Debug.Log( $"________FlightController Angular Velocity: {localAngularVelocity} " +
                    $"PitchErr: {pitchError} " +
                    $"PitchCorr: {mixerInput.PitchCorrection}");

        _mixer.Mix(_simulation.Motors,mixerInput);
    }
}