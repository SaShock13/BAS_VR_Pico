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

    public List<MissionStepData> Steps = new();
}