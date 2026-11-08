using UnityEngine;

public enum ControllerButtonHintId
{
    None,
    [InspectorName("PartsMenu (Menu, левый)")] PartsMenu,
    [InspectorName("LeftPrimary (X)")] LeftPrimary,
    [InspectorName("LeftSecondary (Y)")] LeftSecondary,
    LeftTrigger,
    LeftGrip,
    [InspectorName("RightPrimary (A)")] RightPrimary,
    [InspectorName("RightSecondary (B)")] RightSecondary,
    RightTrigger,
    RightGrip,
    [InspectorName("RightThumbstick (нажатие)")] RightThumbstick,
    [InspectorName("LeftThumbstick (нажатие)")] LeftThumbstick
}
