using System.Collections.Generic;
using System.Linq;

public sealed class ValidationGroupResult
{
    public string GroupName;

    public float Score;

    public readonly List<ValidationMessage> Messages =
        new();

    public bool IsPassed =>
        Messages.All(x => x.Severity != ValidationSeverity.Error);
}