using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Mission System/Mission Definition",
    fileName = "MissionDefinition")]
public class MissionDefinition : ScriptableObject
{
    public string MissionId;

    public string MissionName;

    [TextArea(3, 10)]
    public string Briefing;

    [Min(0f), Tooltip("Seconds. 0 uses the default of 3 seconds.")]
    public float BriefingNotificationDuration;

    public List<MissionStepData> Steps = new();

    [Header("Completion feedback")]
    [TextArea(2, 4)]
    public string CompletionMessage = "Поздравляю, вы завершили миссию!";
    public bool PlayCompletionSound = true;
    public SFXId CompletionSound = SFXId.MissionCompleteSound;

    [Min(0f), Tooltip("Seconds. 0 uses the default of 4 seconds.")]
    public float CompletionNotificationDuration;
}
