using System.Linq;
using Zenject;

public sealed class StructuralValidator
{

    [Inject] private IDroneAnalyzer _analyzer;

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

        ValidateBattery(drone, result);

        ValidateFlightController(
            drone,
            result);

        ValidateMotors(
            drone,
            result);

        ValidatePropellers(
            drone,
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
    DroneDomainState drone,
    ValidationGroupResult result)
    {
        if (!_analyzer.HasPart(drone, PartType.Battery))
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствует аккумулятор"));
        }
    }

    private void ValidateFlightController(
    DroneDomainState drone,
    ValidationGroupResult result)
    {
        if (!_analyzer.HasPart(drone, PartType.FlyController))
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствует контроллер управления"));
        }
    }

    private void ValidateMotors(
    DroneDomainState drone,
    ValidationGroupResult result)
    {
        if (_analyzer.CountParts(drone, PartType.Motor) == 0)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "Отсутствуют двигатели"));
        }
    }

    private void ValidatePropellers(
    DroneDomainState drone,
    ValidationGroupResult result)
    {
        foreach (PartDomainState motor
                 in _analyzer.GetParts(drone, PartType.Motor))
        {
            bool hasPropeller =
                _analyzer.GetParts(drone, PartType.Propeller).Any(
                    p =>
                    p.AttachedPartInstanceId ==
                    motor.PartId);

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