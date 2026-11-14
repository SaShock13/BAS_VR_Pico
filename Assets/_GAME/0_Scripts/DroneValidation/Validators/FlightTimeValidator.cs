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

        if (context.EnforceMissionLimits && context.Requirements.MinFlightTimeMinutes <= 0f)
            return result;

        if (flightTime <
            context.Requirements.MinFlightTimeMinutes)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    GetFailureReason(context, flightTime)));

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

    private static string GetFailureReason(DroneValidationContext context, float flightTime)
    {
        var physics = context.physicsData;
        if (physics.Battery == null || physics.Battery.CapacityMah <= 0f)
            return "Не удалось оценить время полёта: отсутствуют данные о ёмкости аккумулятора.";
        if (physics.HoverThrottle <= 0f || physics.HoverThrottle > 1f)
            return "Не удалось оценить время полёта: тяги моторов недостаточно для висения.";
        if (flightTime <= 0f)
            return "Не удалось оценить время полёта: проверьте ток потребления моторов.";
        return $"Время полёта {flightTime:F1} мин, требуется {context.Requirements.MinFlightTimeMinutes:F1} мин.";
    }
}
