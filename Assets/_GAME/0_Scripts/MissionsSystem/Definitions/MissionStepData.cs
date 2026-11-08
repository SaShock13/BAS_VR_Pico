using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public abstract class MissionStepData : ScriptableObject
{
    [Header("Controller button hint")]
    [Tooltip("Highlight the XR controller button(s) bound to this action. Leave empty to disable.")]
    public InputActionReference ControllerButtonAction;

    [SerializeField, HideInInspector, FormerlySerializedAs("ControllerButtonHint")]
    private ControllerButtonHintId legacyControllerButtonHint;

    [Header("Objective notification")]
    [Min(0f), Tooltip("Seconds. 0 uses the default of 3 seconds.")]
    public float ObjectiveNotificationDuration;

    [Header("Completion feedback")]
    [TextArea(2, 4)]
    public string CompletionMessage = "Отлично, вы сделали это!";
    public bool PlayCompletionSound = true;
    public SFXId CompletionSound = SFXId.MissionStepComplete;

    [Min(0f), Tooltip("Seconds. 0 uses the default of 3 seconds.")]
    public float CompletionNotificationDuration;

    public abstract MissionStep CreateStep(
        SceneMissionBinder binder);
}
