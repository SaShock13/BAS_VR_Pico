public sealed class DroneReadinessService
{
    private readonly StructuralValidator _structural;

    private readonly DroneValidationService _flight;

    private readonly DronePhysicsStatsBuilder _statsBuilder;

    public DroneReadinessService(
        StructuralValidator structural,
        DronePhysicsStatsBuilder statsBuilder,
        DroneValidationService flight)
    {
        _structural = structural;
        _statsBuilder = statsBuilder;
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

        context.physicsData = _statsBuilder.Build(context.Parts, context.droneTransform);

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