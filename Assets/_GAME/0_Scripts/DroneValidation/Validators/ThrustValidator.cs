using UnityEngine;
using Zenject;

public sealed class ThrustValidator
    : IDroneValidator
{
    private readonly IDroneStatsCalculator _stats;
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Тяговооруженность";

    public float Weight => 25f;

    public ThrustValidator(
        IDroneStatsCalculator stats)
    {
        _stats = stats;
    }

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {
        float thrust =
            _stats.CalculateTotalThrust(
                context.Drone);

        float mass =
            _stats.CalculateMass(
                context.Drone);

        float ratio =
            thrust / mass;

        ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };

        if (ratio <
            context.Requirements.MinThrustToWeightRatio)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Недостаточная тяговооруженность ({ratio:F2})"));

            result.Score = 0;
        }

        return result;
    }
}