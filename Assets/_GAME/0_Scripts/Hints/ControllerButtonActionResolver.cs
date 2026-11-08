using System;
using UnityEngine.InputSystem;

// Resolve only on step/binding changes, never every frame. IDs remain internal
// addresses for the prepared controller meshes, not settings in mission data.
public static class ControllerButtonActionResolver
{
    public static uint Resolve(InputAction action)
    {
        if (action == null)
            return 0;
        uint result = 0;
        var map = action.actionMap;
        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath)
                || !Matches(action.bindingMask, binding)
                || (map != null && !Matches(map.bindingMask, binding))
                || (map != null && map.asset != null && !Matches(map.asset.bindingMask, binding)))
                continue;
            result |= ResolvePath(binding.effectivePath);
        }
        return result;
    }

    private static bool Matches(InputBinding? mask, InputBinding binding) =>
        !mask.HasValue || mask.Value.Matches(binding);

    public static uint ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return 0;
        string layout = InputControlPath.TryGetDeviceLayout(path);
        if (string.IsNullOrEmpty(layout)
            || (!string.Equals(layout, "XRController", StringComparison.OrdinalIgnoreCase)
                && !InputSystem.IsFirstLayoutBasedOnSecond(layout, "XRController")))
            return 0;
        int slash = path.IndexOf('/', path[0] == '/' ? 1 : 0);
        if (slash < 0)
            return 0;
        int end = path.IndexOf('/', slash + 1);
        string control = path.Substring(slash + 1, (end < 0 ? path.Length : end) - slash - 1)
            .Trim('{', '}').ToLowerInvariant();
        bool left = path.IndexOf("{LeftHand}", StringComparison.OrdinalIgnoreCase) >= 0;
        bool right = path.IndexOf("{RightHand}", StringComparison.OrdinalIgnoreCase) >= 0;
        // A binding without a hand usage can target either connected controller.
        if (!left && !right)
            left = right = true;
        uint result = 0;
        if (left) result |= Mask(ResolveControl(control, true));
        if (right) result |= Mask(ResolveControl(control, false));
        return result;
    }

    private static ControllerButtonHintId ResolveControl(string control, bool left)
    {
        switch (control)
        {
            case "menu": case "menubutton": case "start":
                return left ? ControllerButtonHintId.PartsMenu : ControllerButtonHintId.None;
            case "primarybutton": case "abutton": case "xbutton": case "buttonsouth": case "buttona": case "buttonx":
                return left ? ControllerButtonHintId.LeftPrimary : ControllerButtonHintId.RightPrimary;
            case "secondarybutton": case "bbutton": case "ybutton": case "buttoneast": case "buttonb": case "buttony":
                return left ? ControllerButtonHintId.LeftSecondary : ControllerButtonHintId.RightSecondary;
            case "trigger": case "triggerbutton": case "triggerpressed":
                return left ? ControllerButtonHintId.LeftTrigger : ControllerButtonHintId.RightTrigger;
            case "grip": case "gripbutton": case "grippressed": case "squeeze": case "squeezepressed":
                return left ? ControllerButtonHintId.LeftGrip : ControllerButtonHintId.RightGrip;
            case "primary2daxisclick": case "thumbstickclicked": case "thumbstickbutton": case "joystickbutton":
            case "primary2daxis": case "thumbstick": case "joystick":
                return left ? ControllerButtonHintId.LeftThumbstick : ControllerButtonHintId.RightThumbstick;
            default:
                return ControllerButtonHintId.None;
        }
    }

    public static uint Mask(ControllerButtonHintId button) =>
        button == ControllerButtonHintId.None ? 0u : 1u << (int)button;

    public static ControllerButtonHintId First(uint mask)
    {
        for (int i = 1; i < 32; i++)
            if ((mask & (1u << i)) != 0)
                return (ControllerButtonHintId)i;
        return ControllerButtonHintId.None;
    }
}
