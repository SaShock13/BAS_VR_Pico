public interface IDroneValidator
{
    string GroupName { get; }

    float Weight { get; }

    ValidationGroupResult Validate(
        DroneValidationContext context);
}