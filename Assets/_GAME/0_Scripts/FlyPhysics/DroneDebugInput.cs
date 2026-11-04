using UnityEngine;
using UnityEngine.InputSystem;

public class DroneDebugInput : MonoBehaviour
{
    public FlightInput CurrentInput { get; private set; }
    public float Vertical { get; private set; }
    private InputAction _left, _right;

    private void Awake()
    {
        _left = new InputAction("FlightLeft", InputActionType.Value, "<XRController>{LeftHand}/primary2DAxis");
        _right = new InputAction("FlightRight", InputActionType.Value, "<XRController>{RightHand}/primary2DAxis");
    }
    private void OnEnable() { _left.Enable(); _right.Enable(); }
    private void OnDisable() { _left.Disable(); _right.Disable(); CurrentInput = default; Vertical = 0; }
    private void OnDestroy() { _left.Dispose(); _right.Dispose(); }
    private void Update()
    {
        var left = _left.ReadValue<Vector2>();
        var right = _right.ReadValue<Vector2>();
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            left.x += (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0);
            left.y += (keyboard.spaceKey.isPressed ? 1 : 0) - (keyboard.leftCtrlKey.isPressed ? 1 : 0);
            right.x += (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            right.y += (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
        }
        Vertical = Mathf.Clamp(left.y, -1, 1);
        CurrentInput = new FlightInput { Throttle = Mathf.Clamp01(Vertical) * 0.8f,
            Pitch = Mathf.Clamp(right.y, -1, 1), Roll = Mathf.Clamp(right.x, -1, 1), Yaw = -Mathf.Clamp(left.x, -1, 1) };
    }
}
