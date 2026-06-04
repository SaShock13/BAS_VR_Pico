using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

public sealed class FlightTimeValidator
    : IDroneValidator
{
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Время полета";

    public float Weight => 10f;
      

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        float flightTime = context.physicsData.EstimatedFlightTimeMinutes;

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
                    $"Недостаточное время полета.Время полета : {flightTime} мин, Требуется {context.Requirements.MinFlightTimeMinutes:F1} мин. Увеличьте емкость аккумулятора или снизьте потребление "));

            result.Score = 0;
        }
        else
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Info,
                    $"Примерное время полета : {flightTime} мин"));
            _logger.Log($"Время полета : {flightTime}");
        }



        return result;
    }
}