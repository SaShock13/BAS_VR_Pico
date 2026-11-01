using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public sealed class AssemblyDetailModeInput : MonoBehaviour
{
    [SerializeField] private InputAction detailMode = new InputAction("Access drone parts", InputActionType.Button);
    private IEventBus _eventBus;
    private bool _enabledByThisView;

    [Inject]
    public void Construct(IEventBus eventBus) => _eventBus = eventBus;

    private void Awake()
    {
        detailMode.wantsInitialStateCheck = true;
        if (detailMode.bindings.Count != 0) return;
        detailMode.AddBinding("<XRController>{LeftHand}/primaryButton");
        detailMode.AddBinding("<Keyboard>/space");
    }

    private void OnEnable()
    {
        detailMode.performed += OnPressed;
        detailMode.canceled += OnReleased;
        _enabledByThisView = !detailMode.enabled;
        if (_enabledByThisView) detailMode.Enable();
    }

    private void OnPressed(InputAction.CallbackContext context) => Publish(true);
    private void OnReleased(InputAction.CallbackContext context) => Publish(false);

    private void Publish(bool held)
    {
        _eventBus?.Publish(new SetAssemblyDetailModeRequest { IsHeld = held });
    }

    private void OnDisable()
    {
        detailMode.performed -= OnPressed;
        detailMode.canceled -= OnReleased;
        if (_enabledByThisView) detailMode.Disable();
        Publish(false);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) Publish(false);
    }

    private void OnDestroy() => detailMode.Dispose();
}
