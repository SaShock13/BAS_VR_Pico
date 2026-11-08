using System;
using UnityEngine.InputSystem;

public sealed class ControllerButtonHintRequest : IAppEvent
{
    public string EventId => nameof(ControllerButtonHintRequest);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public InputAction Action { get; }

    public ControllerButtonHintRequest(InputAction action) => Action = action;
}

public sealed class PartsMenuVisibilityChangedEvent : IAppEvent
{
    public string EventId => nameof(PartsMenuVisibilityChangedEvent);
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsVisible { get; }
    public InputAction MenuAction { get; }

    public PartsMenuVisibilityChangedEvent(bool isVisible, InputAction menuAction = null)
    {
        IsVisible = isVisible;
        MenuAction = menuAction;
    }
}
