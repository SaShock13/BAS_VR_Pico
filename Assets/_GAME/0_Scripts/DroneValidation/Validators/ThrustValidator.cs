using UnityEngine;
using Zenject;

public sealed class ThrustValidator
    : IDroneValidator
{
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Тяговооруженность";

    public float Weight => 25f;


    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {
        float thrust = context.physicsData.MaxAvailableThrust;

        float mass = context.physicsData.TotalMass;

        float ratio =
            thrust / mass;

        ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };


        if(ratio <= 1 )
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Недостаточная тяговооруженность ({ratio:F2}) - Дрон не сможет взлететь"));

            result.Score = 0;
        }

        else if (ratio <
            context.Requirements.MinThrustToWeightRatio)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Недостаточная тяговооруженность ({ratio:F2}) для данных условий"));

            result.Score = 0;
        }
        else if (ratio < 1.3f )
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Info,
                    $"Плохая тяговооруженность ({ratio:F2})  - очень тяжелый дрон"));

            _logger.Log($"Допустимая тяговооруженность ({ratio:F2}) thrust {thrust} with mass {mass} ");
        }

        else if (ratio < 1.8f )
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Info,
                    $"Допустимая тяговооруженность ({ratio:F2})"));

            _logger.Log($"Допустимая тяговооруженность ({ratio:F2}) thrust {thrust} with mass {mass} ");
        }
            
        else 
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Info,
                    $"Отличная тяговооруженность ({ratio:F2})"));

            _logger.Log($"Отличная тяговооруженность ({ratio:F2}) thrust {thrust} with mass {mass} ");
        }
            

        return result;
    }
}