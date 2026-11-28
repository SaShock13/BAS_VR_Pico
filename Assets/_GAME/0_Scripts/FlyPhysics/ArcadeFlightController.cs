using UnityEngine;

// A single body, direct movement and held attitude; no motor mixer or PID.
public sealed class ArcadeFlightController : MonoBehaviour
{
    [SerializeField] private ArcadeFlightSettings settings;
    private Rigidbody _body;
    private DroneDebugInput _input;

    public void Initialize(Rigidbody body, DroneDebugInput input, ArcadeFlightSettings flightSettings)
    {
        if (flightSettings == null)
        {
            Debug.LogError("Assign ArcadeFlightSettings on FlightSceneBootsttrap before launching arcade flight.", this);
            enabled = false;
            return;
        }
        settings = flightSettings;
        _body = body;
        _input = input;
        body.isKinematic = false;
        body.useGravity = true;
        body.centerOfMass = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.linearDamping = settings.Drag;
        body.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (_body == null || _input == null || settings == null) return;
        // Read the shared asset each physics step so Inspector edits apply during flight.
        float maxTilt = settings.MaxTilt;
        float tiltSpeed = settings.TiltSpeed;
        float drag = settings.Drag;
        if (_body.linearDamping != drag) _body.linearDamping = drag;
        if (!_input.MotorsRunning) return;
        var input = _input.CurrentInput;
        float dt = Time.fixedDeltaTime;
        var rates = FlightControlRates.Calculate(_input.ControlMode, input, _body.rotation,
            Vector2.one * tiltSpeed, maxTilt);
        var attitude = _body.rotation * Quaternion.Euler(
            rates.x * dt, -input.Yaw * settings.YawSpeed * dt, rates.y * dt);
        _body.MoveRotation(attitude);
        float hoverThrust = Physics.gravity.magnitude;
        float stick = _input.Vertical;
        float thrust = hoverThrust + stick * (stick < 0f ? hoverThrust : settings.VerticalAcceleration);
        _body.AddForce(attitude * Vector3.up * thrust, ForceMode.Acceleration);
    }
}
