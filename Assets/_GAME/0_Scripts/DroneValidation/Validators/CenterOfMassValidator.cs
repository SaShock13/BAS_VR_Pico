using UnityEngine;
using Zenject;

public sealed class CenterOfMassValidator
    : IDroneValidator
{
    private readonly IDroneStatsCalculator _stats;
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Центр тяжести";

    public float Weight => 15f;

    public CenterOfMassValidator(
        IDroneStatsCalculator stats)
    {
        _stats = stats;
    }

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        Vector3 offset =
            _stats.CalculateCenterOfMassOffset(
                context.Drone);

        float distance =
            offset.magnitude;

        ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };

        if (distance >
            context.Requirements.MaxCenterOfMassOffset)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Warning,
                    $"Смещение центра тяжести {distance:F2} м"));

            result.Score = 50;
        }

        return result;
    }
}