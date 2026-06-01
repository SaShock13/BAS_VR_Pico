using System.Collections.Generic;
using System.Linq;

public sealed class DroneValidationResult
{
    public readonly List<ValidationGroupResult> Groups =
        new();

    public float TotalScore;

    public bool IsReady =>
        Groups.All(x => x.IsPassed);
}