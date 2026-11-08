using System;

public interface IControllerButtonHintService
{
    ControllerButtonHintId CurrentButton { get; }
    bool IsHighlighted(ControllerButtonHintId button);
    event Action<ControllerButtonHintId> Changed;
}
