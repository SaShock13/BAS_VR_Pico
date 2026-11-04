using System.Collections.Generic;
using System.Text;

public sealed class DroneReadinessResult
{
    public bool IsReady;

    public float TotalScore;

    public List<ValidationGroupResult> Groups =
        new();

    public string StatusText => IsReady ? "Валидация пройдена" : "Валидация не пройдена";

    public string GetDetailsText()
    {
        var details = new StringBuilder();
        foreach (var group in Groups)
            foreach (var message in group.Messages)
                if (message.Severity != ValidationSeverity.Info)
                    details.AppendLine(message.Message);
        return details.ToString().TrimEnd();
    }
}
