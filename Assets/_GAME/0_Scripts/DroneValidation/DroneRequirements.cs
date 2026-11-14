[System.Serializable]
public sealed class DroneRequirements
{
    public int MinMotorCount;
    public int MinPropellerCount;

    public bool RequireBattery;
    public bool RequireFrame;
    public bool RequireCamera;

    public float MinFlightTimeMinutes;

    public float MinThrustToWeightRatio;

    [UnityEngine.Tooltip("Максимальное смещение центра тяжести, м. 0 — без дополнительного ограничения миссии.")]
    public float MaxCenterOfMassOffset;

    [UnityEngine.Tooltip("Проверять пересечения деталей, установленных в монтажные зоны.")]
    public bool CheckCollisions = true;

    public string Describe()
    {
        var items = new System.Collections.Generic.List<string>();
        if (RequireFrame) items.Add("рама");
        if (RequireBattery) items.Add("аккумулятор");
        if (RequireCamera) items.Add("камера");
        if (MinMotorCount > 0) items.Add($"моторов ≥ {MinMotorCount}");
        if (MinPropellerCount > 0) items.Add($"пропеллеров ≥ {MinPropellerCount}");
        if (MinFlightTimeMinutes > 0) items.Add($"полёт ≥ {MinFlightTimeMinutes:0.##} мин");
        if (MinThrustToWeightRatio > 0) items.Add($"тяговооружённость ≥ {MinThrustToWeightRatio:0.##}");
        if (MaxCenterOfMassOffset > 0) items.Add($"смещение центра тяжести ≤ {MaxCenterOfMassOffset:0.##} м");
        if (CheckCollisions) items.Add("без пересечений в монтажных зонах");
        return string.Join(", ", items);
    }
}
