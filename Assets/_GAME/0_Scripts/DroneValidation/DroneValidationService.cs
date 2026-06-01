using System.Collections.Generic;
using System.Linq;

public sealed class DroneValidationService
{
    private readonly List<IDroneValidator>
        _validators;

    public DroneValidationService(
        IEnumerable<IDroneValidator> validators)
    {
        _validators = validators.ToList();
    }

    public DroneValidationResult Validate(
        DroneValidationContext context)
    {
        DroneValidationResult result =
            new();

        float weightSum = 0;
        float scoreSum = 0;

        foreach (IDroneValidator validator
                 in _validators)
        {
            ValidationGroupResult group =
                validator.Validate(context);

            result.Groups.Add(group);

            scoreSum +=
                group.Score *
                validator.Weight;

            weightSum +=
                validator.Weight;
        }

        result.TotalScore =
            scoreSum / weightSum;

        return result;
    }
}