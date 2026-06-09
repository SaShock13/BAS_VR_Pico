using System.Collections.Generic;

public static class MissionFactory
{
    public static MissionRuntime CreateRuntime(
        MissionDefinition definition,
        SceneMissionBinder binder)
    {
        List<MissionStep> steps = new();

        foreach (var stepData in definition.Steps)
        {
            steps.Add(stepData.CreateStep(binder));
        }

        return new MissionRuntime(
            steps,
            new List<MissionCondition>());
    }
}