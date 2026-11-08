using System;
using UnityEngine.InputSystem;
using Zenject;

public sealed class ControllerButtonHintService : IControllerButtonHintService, IInitializable, IDisposable
{
    private readonly IEventBus _events;
    private IDisposable _requestSubscription;
    private IDisposable _menuSubscription;
    private InputAction _action;
    private InputAction _menuAction;
    private uint _requested;
    private bool _partsMenuVisible;
    public event Action<ControllerButtonHintId> Changed;

    private uint CurrentMask => _partsMenuVisible && _action != null && _menuAction != null
        && _action.id == _menuAction.id ? 0u : _requested;
    public ControllerButtonHintId CurrentButton => ControllerButtonActionResolver.First(CurrentMask);
    public bool IsHighlighted(ControllerButtonHintId button) =>
        (CurrentMask & ControllerButtonActionResolver.Mask(button)) != 0;

    public ControllerButtonHintService(IEventBus events) => _events = events;

    public void Initialize()
    {
        if (_requestSubscription != null)
            return;
        _requestSubscription = _events.Subscribe<ControllerButtonHintRequest>(request =>
        {
            uint previous = CurrentMask;
            _action = request.Action;
            _requested = ControllerButtonActionResolver.Resolve(_action);
            NotifyChanged(previous);
        });
        _menuSubscription = _events.Subscribe<PartsMenuVisibilityChangedEvent>(menu =>
        {
            uint previous = CurrentMask;
            _partsMenuVisible = menu.IsVisible;
            _menuAction = menu.MenuAction;
            NotifyChanged(previous);
        });
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnActionChange(object changed, InputActionChange change)
    {
        if (_action == null || change != InputActionChange.BoundControlsChanged)
            return;
        var map = _action.actionMap;
        if (!ReferenceEquals(changed, _action) && !ReferenceEquals(changed, map)
            && !(map != null && ReferenceEquals(changed, map.asset)))
            return;
        uint previous = CurrentMask;
        _requested = ControllerButtonActionResolver.Resolve(_action);
        NotifyChanged(previous);
    }

    private void NotifyChanged(uint previous)
    {
        if (previous != CurrentMask)
            Changed?.Invoke(CurrentButton);
    }

    public void Dispose()
    {
        _requestSubscription?.Dispose();
        _menuSubscription?.Dispose();
        _requestSubscription = _menuSubscription = null;
        InputSystem.onActionChange -= OnActionChange;
        uint previous = CurrentMask;
        _action = _menuAction = null;
        _requested = 0;
        _partsMenuVisible = false;
        NotifyChanged(previous);
    }
}
