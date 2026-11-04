using UnityEngine;

// A single body, direct movement and held attitude; no motor mixer or PID.
public sealed class ArcadeFlightController : MonoBehaviour
{
    [SerializeField] private ArcadeFlightSettings settings;
    private Rigidbody _body;
    private DroneDebugInput _input;
    private float _pitch, _roll, _yaw;

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
        _yaw = body.rotation.eulerAngles.y;
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
        var input = _input.CurrentInput;
        float dt = Time.fixedDeltaTime;
        _pitch = Mathf.Clamp(_pitch + input.Pitch * tiltSpeed * dt, -maxTilt, maxTilt);
        _roll = Mathf.Clamp(_roll - input.Roll * tiltSpeed * dt, -maxTilt, maxTilt);
        _yaw -= input.Yaw * settings.YawSpeed * dt;
        var heading = Quaternion.Euler(0f, _yaw, 0f);
        _body.MoveRotation(heading * Quaternion.Euler(_pitch, 0f, _roll));
        var horizontal = heading * new Vector3(-_roll / maxTilt, 0f, _pitch / maxTilt);
        // Neutral stick hovers; attitude remains where the pilot left it.
        var acceleration = horizontal * settings.HorizontalAcceleration + Vector3.up * (_input.Vertical * settings.VerticalAcceleration);
        _body.AddForce(acceleration - Physics.gravity, ForceMode.Acceleration);
    }
}
