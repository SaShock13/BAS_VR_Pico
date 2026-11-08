using UnityEngine;

[CreateAssetMenu(menuName = "Mission System/Steps/Create Part")]
public sealed class CreatePartStepData : MissionStepData
{
    public PartType RequiredPartType;
    public string Description = "Создайте деталь через меню деталей";

    public override MissionStep CreateStep(SceneMissionBinder binder)
    {
        return new CreatePartStep(RequiredPartType, Description, binder._AssemblySystem,
            binder.EventBus, binder._hintScenario);
    }
}
