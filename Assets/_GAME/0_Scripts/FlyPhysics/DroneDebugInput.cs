using UnityEngine;

public class DroneDebugInput : MonoBehaviour
{
    [SerializeField]
    private DronePhysicsSimulation _simulation;

    private readonly DroneMixer _mixer =
        new();

    private FlightInput _input;

    private void Update()
    {
        ReadInput();

        _mixer.Mix(
            _simulation.Motors,
            _input);
    }

    private void ReadInput()
    {
        _input.Throttle = 0f;

        if (Input.GetKey(KeyCode.Space))
        {
            _input.Throttle = 0.8f;
        }

        _input.Pitch =
            Input.GetAxis("Vertical") * 0.5f;

        _input.Roll =
            Input.GetAxis("Horizontal") * 0.5f * -1f;

        _input.Yaw = 0f;

        if (Input.GetKey(KeyCode.Q))   /// todo доработать!!!
        {
            _input.Yaw = -0.5f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            _input.Yaw = 0.5f;
        }
    }
}