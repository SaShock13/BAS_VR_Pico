using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class LeftHandMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuPrefab;
    [SerializeField] private GameObject sceneMenu;
    [SerializeField] private GameObject systemMenu;
    [SerializeField] private Transform menuTransform;
    [SerializeField] private Transform initMenuTransform;
    [SerializeField] private InputActionReference leftHandMenuAction;
    [Header("Settings")]
    [SerializeField, Range(0.1f, 0.5f)] private float appearDuration = 0.3f;
    [SerializeField, Min(0.1f)] private float longPressDuration = 1f;

    private GameObject menuObj, _visibleMenu;
    private DiContainer _container;
    private IEventBus _events;
    private Coroutine _hold, _animation;
    private InputAction _pressedAction;
    private double _pressedAt;
    private bool _longTriggered;
    private static readonly Vector3 HiddenScale = Vector3.one * .02f;
#if UNITY_EDITOR
    private InputAction _editorMenuAction;
#endif
    public bool MenuEnabled { get => _visibleMenu != null; set { if (!value) HideMenu(); else if (!MenuEnabled) ToggleMenu(); } }
    public bool PartsMenuEnabled => menuObj != null && _visibleMenu == menuObj;

    [Inject]
    public void Construct(DiContainer container, IEventBus eventBus)
    {
        _container = container;
        _events = eventBus;
        NotifyPartsMenuVisibility();
    }

    private void NotifyPartsMenuVisibility() =>
        _events?.Publish(new PartsMenuVisibilityChangedEvent(PartsMenuEnabled,
            leftHandMenuAction != null ? leftHandMenuAction.action : null));

    private void Awake()
    {
#if UNITY_EDITOR
        _editorMenuAction = new InputAction("Editor hand menu", InputActionType.Button, "<Keyboard>/m");
#endif
    }
    private void OnEnable()
    {
        Subscribe(leftHandMenuAction != null ? leftHandMenuAction.action : null);
#if UNITY_EDITOR
        Subscribe(_editorMenuAction);
        _editorMenuAction.Enable();
#endif
    }
    private void Subscribe(InputAction action)
    {
        if (action == null) return;
        action.performed += OnPress;
        action.canceled += OnRelease;
    }
    private void Unsubscribe(InputAction action)
    {
        if (action == null) return;
        action.performed -= OnPress;
        action.canceled -= OnRelease;
    }
    private void OnDisable()
    {
        Unsubscribe(leftHandMenuAction != null ? leftHandMenuAction.action : null);
#if UNITY_EDITOR
        Unsubscribe(_editorMenuAction);
        _editorMenuAction.Disable();
#endif
        CancelPress();
        if (_animation != null) StopCoroutine(_animation);
        _animation = null;
        HideImmediately(menuObj);
        HideImmediately(systemMenu);
        _visibleMenu = null;
        NotifyPartsMenuVisibility();
    }
    private void Start()
    {
        menuObj = sceneMenu != null ? sceneMenu : _container.InstantiatePrefab(menuPrefab, menuTransform);
        Prepare(menuObj);
        Prepare(systemMenu);
        NotifyPartsMenuVisibility();
    }
    private static void Prepare(GameObject target)
    {
        if (target == null) return;
        if (target.GetComponent<CanvasGroup>() == null) target.AddComponent<CanvasGroup>();
        HideImmediately(target);
    }
    private static void HideImmediately(GameObject target)
    {
        if (target == null) return;
        SetInteraction(target, false);
        target.transform.localScale = HiddenScale;
        target.SetActive(false);
    }
    private void OnPress(InputAction.CallbackContext context)
    {
        if (menuObj == null || _pressedAction != null || !context.action.IsPressed()) return;
        _pressedAction = context.action;
        _pressedAt = Time.realtimeSinceStartupAsDouble;
        _longTriggered = false;
        _hold = StartCoroutine(WaitForLongPress());
    }
    private IEnumerator WaitForLongPress()
    {
        yield return new WaitForSecondsRealtime(longPressDuration);
        _hold = null;
        if (_pressedAction == null || !_pressedAction.enabled || !_pressedAction.IsPressed()) yield break;
        _longTriggered = true;
        ToggleSystemMenu();
    }
    private void OnRelease(InputAction.CallbackContext context)
    {
        if (_pressedAction != context.action) return;
        bool valid = context.action.enabled && Application.isFocused;
        bool longPress = _longTriggered || Time.realtimeSinceStartupAsDouble - _pressedAt >= longPressDuration;
        bool alreadyTriggered = _longTriggered;
        CancelPress();
        if (!valid || alreadyTriggered) return;
        if (longPress) ToggleSystemMenu();
        else ToggleMenu();
    }
    private void CancelPress()
    {
        if (_hold != null) StopCoroutine(_hold);
        _hold = null;
        _pressedAction = null;
        _longTriggered = false;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelPress(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelPress(); }

    public void ToggleMenu()
    {
        if (systemMenu != null && _visibleMenu == systemMenu)
        {
            HideMenu();
            return;
        }
        Toggle(menuObj);
    }
    public void ToggleSystemMenu() => Toggle(systemMenu);
    private void Toggle(GameObject target)
    {
        if (target == null) return;
        if (_visibleMenu == target) { HideMenu(); return; }
        if (_animation != null) StopCoroutine(_animation);
        HideImmediately(menuObj);
        HideImmediately(systemMenu);
        _visibleMenu = target;
        NotifyPartsMenuVisibility();
        _animation = StartCoroutine(Show(target));
    }
    private IEnumerator Show(GameObject target)
    {
        SetInteraction(target, false);
        target.SetActive(true);
        float elapsed = 0;
        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            target.transform.SetPositionAndRotation(Vector3.Lerp(initMenuTransform.position, menuTransform.position, t),
                Quaternion.Slerp(initMenuTransform.rotation, menuTransform.rotation, t));
            target.transform.localScale = Vector3.Lerp(HiddenScale, Vector3.one, t);
            yield return null;
        }
        target.transform.SetPositionAndRotation(menuTransform.position, menuTransform.rotation);
        target.transform.localScale = Vector3.one;
        SetInteraction(target, true);
        _animation = null;
    }
    public void HideMenu()
    {
        if (_visibleMenu == null) return;
        if (_animation != null) StopCoroutine(_animation);
        var target = _visibleMenu;
        _visibleMenu = null;
        NotifyPartsMenuVisibility();
        SetInteraction(target, false);
        _animation = StartCoroutine(Hide(target));
    }
    private IEnumerator Hide(GameObject target)
    {
        var scale = target.transform.localScale;
        var position = target.transform.position;
        float elapsed = 0;
        while (elapsed < appearDuration * .5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / (appearDuration * .5f));
            target.transform.position = Vector3.Lerp(position, initMenuTransform.position, t);
            target.transform.localScale = Vector3.Lerp(scale, HiddenScale, t);
            yield return null;
        }
        HideImmediately(target);
        _animation = null;
    }
    private static void SetInteraction(GameObject target, bool enabled)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) return;
        group.interactable = enabled;
        group.blocksRaycasts = enabled;
    }
    private void OnDestroy()
    {
#if UNITY_EDITOR
        _editorMenuAction?.Dispose();
#endif
    }
}
