using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneDebugInput : MonoBehaviour
{
    public FlightInput CurrentInput { get; private set; }
    public float Vertical { get; private set; }
    public event Action ViewChangeRequested;
    private InputAction _left, _right;
    private InputAction _viewChange;
    private InputAction _modeSwitch;
    public FlightControlMode ControlMode { get; private set; }
    public static event Action<FlightControlMode> ControlModeSelected;
    public static void SelectControlMode(FlightControlMode mode) => ControlModeSelected?.Invoke(mode);

    private void Awake()
    {
        ControlMode = FlightSession.Mode == FlightMode.Arcade ? FlightControlMode.AcroTrainer : FlightControlMode.Acro;
        _modeSwitch = InputSystem.actions?.FindAction("Flight/ModeSwitch");
        if (_modeSwitch == null) Debug.LogError("Missing Flight/ModeSwitch in project-wide Input Actions.", this);
        _left = new InputAction("FlightLeft", InputActionType.Value, "<XRController>{LeftHand}/primary2DAxis");
        _right = new InputAction("FlightRight", InputActionType.Value, "<XRController>{RightHand}/primary2DAxis");
        _viewChange = new InputAction("ViewChange", InputActionType.Button);
        _viewChange.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
        _viewChange.AddBinding("<Keyboard>/pageDown");
    }

    private void OnEnable()
    {
        ControlModeSelected += ApplyControlMode;
        if (_modeSwitch != null)
        {
            _modeSwitch.performed += OnModeSwitch;
            _modeSwitch.Enable();
        }
        _left.Enable();
        _right.Enable();
        _viewChange.performed += OnViewChange;
        _viewChange.Enable();
    }

    private void OnDisable()
    {
        ControlModeSelected -= ApplyControlMode;
        if (_modeSwitch != null) _modeSwitch.performed -= OnModeSwitch;
        _viewChange.performed -= OnViewChange;
        _viewChange.Disable();
        _left.Disable();
        _right.Disable();
        CurrentInput = default;
        Vertical = 0;
    }

    private void OnDestroy()
    {
        _left.Dispose();
        _right.Dispose();
        _viewChange.Dispose();
    }

    private void OnViewChange(InputAction.CallbackContext context) => ViewChangeRequested?.Invoke();
    private void OnModeSwitch(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return;
        ControlMode = ControlMode == FlightControlMode.Acro ? FlightControlMode.Angle : ControlMode + 1;
    }
    private void ApplyControlMode(FlightControlMode mode) => ControlMode = mode;
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
