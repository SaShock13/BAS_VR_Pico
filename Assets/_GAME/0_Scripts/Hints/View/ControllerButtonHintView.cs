using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

// Stays on the SDK model loader; its surface visual is a sibling so reconnects
// can replace the model without destroying the hint component or its assets.
public sealed class ControllerButtonHintView : MonoBehaviour
{
    [SerializeField] private ControllerButtonHintId button = ControllerButtonHintId.PartsMenu;
    [SerializeField] private ControllerButtonPressVisual visual;
    [SerializeField] private InputActionReference isTrackedAction;
    [SerializeField] private InputActionReference buttonAction;
    [SerializeField, Tooltip("Enable only for dedicated hint-observation actions, never shared gameplay actions.")]
    private bool manageButtonAction;
    [SerializeField] private string buttonBoneName = "SkinLMenu";
    [SerializeField, Min(0f)] private float hintDelay = 2f;
    private IControllerButtonHintService _service;
    private InputAction _tracking;
    private InputAction _buttonAction;
    private Coroutine _pulse;
    private Coroutine _rebind;
    private bool _showing;
    private bool _subscribed;
    private bool _focused = true;
    private bool _paused;
    private bool _enabledButtonAction;

    [Inject]
    public void Construct(IControllerButtonHintService service)
    {
        Unsubscribe();
        _service = service;
        if (isActiveAndEnabled)
            Subscribe();
    }

    private void Awake() { if (visual != null) visual.Unbind(); }
    private void OnEnable() => Subscribe();

    private void Subscribe()
    {
        if (_subscribed || _service == null)
            return;
        _service.Changed += OnHintChanged;
        _tracking = isTrackedAction != null ? isTrackedAction.action : null;
        _buttonAction = buttonAction != null ? buttonAction.action : null;
        if (_tracking != null)
        {
            _tracking.performed += OnTrackingChanged;
            _tracking.canceled += OnTrackingChanged;
        }
        if (_buttonAction != null)
        {
            _buttonAction.performed += OnButtonChanged;
            _buttonAction.canceled += OnButtonChanged;
        }
        _subscribed = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (_subscribed)
        {
            _service.Changed -= OnHintChanged;
            if (_tracking != null)
            {
                _tracking.performed -= OnTrackingChanged;
                _tracking.canceled -= OnTrackingChanged;
            }
            if (_buttonAction != null)
            {
                _buttonAction.performed -= OnButtonChanged;
                _buttonAction.canceled -= OnButtonChanged;
            }
        }
        _subscribed = false;
        ReleaseButtonAction();
        _tracking = _buttonAction = null;
        StopPulse();
    }

    private void OnHintChanged(ControllerButtonHintId _) => Refresh();
    private void OnTrackingChanged(InputAction.CallbackContext _) => Refresh();
    private void OnButtonChanged(InputAction.CallbackContext _) => UpdatePhysicalPress();
    private void UpdatePhysicalPress()
    {
        if (visual != null)
            visual.SetPhysicalPressed(_buttonAction != null && _buttonAction.enabled
                && _buttonAction.ReadValue<float>() > 0.05f);
    }

    private void OnTransformChildrenChanged()
    {
        if (_showing && isActiveAndEnabled && _rebind == null)
            _rebind = StartCoroutine(RebindAfterModelChange());
    }

    private IEnumerator RebindAfterModelChange()
    {
        yield return null;
        _rebind = null;
        if (_showing)
            BindVisual();
    }

    private void OnApplicationFocus(bool focused) { _focused = focused; Refresh(); }
    private void OnApplicationPause(bool paused) { _paused = paused; Refresh(); }
    private void OnDisable() => Unsubscribe();

    private void Refresh()
    {
        bool tracked = _tracking == null || (_tracking.enabled && _tracking.IsPressed());
        if (!_subscribed || visual == null || !_focused || _paused || !tracked
            || !_service.IsHighlighted(button) || button == ControllerButtonHintId.None)
        {
            StopPulse();
            ReleaseButtonAction();
            return;
        }
        AcquireButtonAction();
        if (_pulse == null && !_showing)
            _pulse = StartCoroutine(Pulse());
    }

    private void AcquireButtonAction()
    {
        if (manageButtonAction && _buttonAction != null && !_buttonAction.enabled)
        {
            _enabledButtonAction = true;
            _buttonAction.Enable();
        }
    }

    private void ReleaseButtonAction()
    {
        if (_enabledButtonAction && _buttonAction != null)
            _buttonAction.Disable();
        _enabledButtonAction = false;
    }

    private IEnumerator Pulse()
    {
        // Always yield once so even a zero delay cannot leave a completed
        // coroutine handle assigned after StartCoroutine returns.
        if (hintDelay > 0f)
            yield return new WaitForSecondsRealtime(hintDelay);
        else
            yield return null;
        _pulse = null;
        _showing = true;
        BindVisual();
    }

    private void BindVisual()
    {
        if (visual == null)
            return;
        // A single-mesh system model has no movable button. Hide safely rather
        // than inventing a press at a detached fallback position.
        visual.Bind(FindBone(transform, buttonBoneName));
        UpdatePhysicalPress();
    }

    private static Transform FindBone(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name)
                return child;
            var found = FindBone(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void StopPulse()
    {
        if (_pulse != null)
            StopCoroutine(_pulse);
        if (_rebind != null)
            StopCoroutine(_rebind);
        _pulse = _rebind = null;
        _showing = false;
        if (visual != null)
            visual.Unbind();
    }
}
