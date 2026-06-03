using System.Linq;
using UnityEngine;
using Zenject;

public sealed class StructuralValidator
{

    [Inject] private IDroneAnalyzer _analyzer;
    [Inject] private IAppLogger _logger;

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {

        ValidationGroupResult result =
            new()
            {
                GroupName =
                    "Конструктивная целостность",
                Score = 100
            };




        DroneDomainState drone =
            context.Drone;


        //ValidateFrame(drone, result);

        ValidateBattery(context, result);

        ValidateFlightController(
            context,
            result);

        ValidateMotors(
            context,
            result);

        ValidatePropellers(
            context,
            result);

        //ValidateAttachment(
        //    drone,
        //    result);

        if (!result.IsPassed)
            result.Score = 0;

        return result;
    }


    //private void ValidateFrame(
    //DroneDomainState drone,
    //ValidationGroupResult result)
    //{
    //    if (!drone.HasFrame)
    //    {
    //        result.Messages.Add(
    //            new ValidationMessage(
    //                ValidationSeverity.Error,
    //                "Отсутствует рама"));
    //    }
    //}

    private void ValidateBattery(
    DroneValidationContext context,
    ValidationGroupResult result)
    {
        if (!_analyzer.HasPart(context, PartType.Battery))
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствует аккумулятор"));
        }
    }

    private void ValidateFlightController(
    DroneValidationContext context,
    ValidationGroupResult result)
    {
        if (!_analyzer.HasPart(context, PartType.FlyController))
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствует контроллер управления"));
        }
    }

    private void ValidateMotors(
    DroneValidationContext context,
    ValidationGroupResult result)
    {
        if (_analyzer.CountParts(context, PartType.Motor) == 0)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствуют двигатели"));
        }
    }

    private void ValidatePropellers(
    DroneValidationContext context,
    ValidationGroupResult result)
    {
        foreach (PartDomainState motor
                 in _analyzer.GetParts(context, PartType.Motor))
        {
            bool hasPropeller =
                _analyzer.GetParts(context, PartType.Propeller).Any(
                    p =>
                    p.AttachedPartInstanceId ==
                    motor.InstanceId);

            if (!hasPropeller)
            {
                result.Messages.Add(
                    new ValidationMessage(
                        ValidationSeverity.Error,
                        $"На моторе {motor.PartId} отсутствует пропеллер"));
            }
        }

        //foreach (PropellerPart propeller
        //         in drone.Propellers)
        //{
        //    bool motorExists =
        //        drone.Motors.Any(
        //            x =>
        //            x.PartId ==
        //            propeller.AttachedMotorId);

        //    if (!motorExists)
        //    {
        //        result.Messages.Add(
        //            new ValidationMessage(
        //                ValidationSeverity.Warning,
        //                $"Пропеллер {propeller.Name} не установлен на двигатель"));
        //    }
        //}
    }

}