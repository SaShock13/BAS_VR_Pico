public sealed class CompletenessValidator : IDroneValidator
{
    private readonly IDroneAnalyzer _analyzer;

    public CompletenessValidator(IDroneAnalyzer analyzer) => _analyzer = analyzer;

    public string GroupName => "Комплектность";
    public float Weight => 40f;

    public ValidationGroupResult Validate(DroneValidationContext context)
    {
        var requirements = context.Requirements;
        var result = new ValidationGroupResult { GroupName = GroupName, Score = 100 };

        if (requirements.RequireFrame && !_analyzer.HasPart(context, DroneAssemblyRules.RootPartType))
            AddError(result, "Отсутствует рама");
        if (requirements.RequireBattery && !_analyzer.HasPart(context, PartType.Battery))
            AddError(result, "Отсутствует аккумулятор");
        if (requirements.RequireCamera && !_analyzer.HasPart(context, PartType.Camera))
            AddError(result, "Отсутствует камера");
        if (_analyzer.CountParts(context, PartType.Motor) < requirements.MinMotorCount)
            AddError(result, $"Недостаточно моторов. Требуется {requirements.MinMotorCount}");
        if (_analyzer.CountParts(context, PartType.Propeller) < requirements.MinPropellerCount)
            AddError(result, $"Недостаточно пропеллеров. Требуется {requirements.MinPropellerCount}");

        return result;
    }

    private static void AddError(ValidationGroupResult result, string message)
    {
        result.Messages.Add(new ValidationMessage(ValidationSeverity.Error, message));
        result.Score = 0;
    }
}
