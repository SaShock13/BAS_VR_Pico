using UnityEngine;
using Zenject;

public sealed class CollisionValidator
    : IDroneValidator
{
    private readonly IDroneAnalyzer _analyzer;
    [Inject] private IAppLogger _logger;
    public string GroupName =>
        "Перекрытие деталей";

    public float Weight => 10f;

    public CollisionValidator(
        IDroneAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public ValidationGroupResult Validate(
        DroneValidationContext context)
    {


       ValidationGroupResult result =
            new()
            {
                GroupName = GroupName,
                Score = 100
            };

        if (!context.Requirements.CheckCollisions)
            return result;

        var collisions =
            _analyzer.FindCollisions(
                context.Drone);

        foreach (string collision
                 in collisions)
        {
            result.Messages.Add(
                new ValidationMessage(
                    ValidationSeverity.Error,
                    collision));
        }

        if (collisions.Count > 0)
            result.Score = 0;

        return result;
    }
}