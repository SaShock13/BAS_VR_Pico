using UnityEngine;
using Zenject;

public sealed class CenterOfMassValidator
    : IDroneValidator
{
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Центр тяжести";

    public float Weight => 15f;


    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        Vector3 offset = context.physicsData.LocalCenterOfMass;
            

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
                    $"Внимание! Смещение центра тяжести {distance:F2} м"));

            result.Score = 50;
        }
        else if (distance > 0.5f)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Критическое смещение центра тяжести {distance:F2} м !!!!"));

            result.Score = 0;
        }
        else
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Info,
                    $"Допустимое Смещение центра тяжести {distance:F2} м"));
            _logger.Log($"Допустимое Смещение центра тяжести {distance:F2} м");
        }

        return result;
    }
}