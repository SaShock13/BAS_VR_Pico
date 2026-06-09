using UnityEngine;

public abstract class MissionStepData : ScriptableObject
{
    public abstract MissionStep CreateStep(
        SceneMissionBinder binder);
}