using System.Collections.Generic;

public sealed class DroneReadinessResult
{
    public bool IsReady;

    public float TotalScore;

    public List<ValidationGroupResult> Groups =
        new();
}