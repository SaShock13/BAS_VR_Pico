public sealed class DroneReadinessService
{
    private readonly StructuralValidator _structural;

    private readonly DroneValidationService _flight;

    public DroneReadinessService(
        StructuralValidator structural,
        DroneValidationService flight)
    {
        _structural = structural;
        _flight = flight;
    }

    public DroneReadinessResult Validate(
        DroneValidationContext context)
    {
        DroneReadinessResult result =
            new();

        ValidationGroupResult structuralResult =
            _structural.Validate(context);

        result.Groups.Add(structuralResult);

        if (!structuralResult.IsPassed)
        {
            result.IsReady = false;
            result.TotalScore = 0;

            return result;
        }

        DroneValidationResult flightResult =
            _flight.Validate(context);

        result.Groups.AddRange(
            flightResult.Groups);

        result.TotalScore =
            flightResult.TotalScore;

        result.IsReady =
            flightResult.IsReady;

        return result;
    }
}