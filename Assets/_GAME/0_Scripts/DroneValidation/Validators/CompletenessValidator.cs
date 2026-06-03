using UnityEngine;
using Zenject;

public sealed class CompletenessValidator
    : IDroneValidator
{
    [Inject] private IAppLogger _logger;

    public string GroupName => "Комплектность";

    public float Weight => 40f;

    private IDroneAnalyzer _analyzer;

    public CompletenessValidator(IDroneAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        _logger.Log($"*****Валидация Комплектность");
        ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };

        DroneDomainState drone =
            context.Drone;

        DroneRequirements req =
            context.Requirements;

        //if (req.RequireFrame &&
        //    !drone.HasFrame)
        //{
        //    result.Messages.Add(
        //        new ValidationMessage(
        //            ValidationSeverity.Error,
        //            "Отсутствует рама"));

        //    result.Score = 0;
        //}

        //if (req.RequireBattery &&
        //    !drone.HasBattery)
        //{
        //    result.Messages.Add(
        //        new ValidationMessage(
        //            ValidationSeverity.Error,
        //            "Отсутствует аккумулятор"));

        //    result.Score = 0;
        //}

        if (req.RequireCamera &&
            _analyzer.HasPart(context, PartType.Camera))
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствует  Камера "));

            result.Score = 0;
        }

        if (_analyzer.CountParts(context, PartType.Motor) <
            req.MinMotorCount)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    $"Недостаточно моторов. Требуется {req.MinMotorCount}"));

            result.Score = 0;
        }




        //if (drone.Propellers.Count <
        //    req.MinPropellerCount)
        //{
        //    result.Messages.Add(
        //        new ValidationMessage(
        //            ValidationSeverity.Error,
        //            $"Недостаточно пропеллеров. Требуется {req.MinPropellerCount}"));

        //    result.Score = 0;
        //}

        return result;
    }
}