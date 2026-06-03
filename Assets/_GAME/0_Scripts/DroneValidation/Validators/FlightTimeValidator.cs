using UnityEngine;
using Zenject;

public sealed class FlightTimeValidator
    : IDroneValidator
{
    private readonly IDroneStatsCalculator _stats;
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Время полета";

    public float Weight => 10f;

    public FlightTimeValidator(
        IDroneStatsCalculator stats)
    {
        _stats = stats;
    }

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        float flightTime =
            _stats.EstimateFlightTimeMinutes(
                context.Drone);

        ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };

        if (flightTime <
            context.Requirements.MinFlightTimeMinutes)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Недостаточное время полета. Требуется {context.Requirements.MinFlightTimeMinutes:F1} мин"));

            result.Score = 0;
        }

        return result;
    }
}