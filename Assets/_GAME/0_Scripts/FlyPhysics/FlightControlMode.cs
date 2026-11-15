using UnityEngine;

public enum FlightControlMode
{
    Angle,
    Horizon,
    AcroTrainer,
    Acro
}

internal static class FlightControlRates
{
    public static Vector2 Calculate(FlightControlMode mode, FlightInput input, Quaternion attitude,
        Vector2 maxRates, float maxTilt)
    {
        var stick = new Vector2(input.Pitch, -input.Roll);
        var acro = Vector2.Scale(stick, maxRates);
        if (mode == FlightControlMode.Acro) return acro;

        var up = Quaternion.Inverse(attitude) * Vector3.up;
        var tilt = new Vector2(Mathf.Atan2(-up.z, up.y), Mathf.Atan2(up.x, up.y)) * Mathf.Rad2Deg;
        var target = Vector2.ClampMagnitude(stick, 1f) * maxTilt;
        if (mode == FlightControlMode.Horizon) target = Vector2.zero;
        var error = target - tilt;
        var level = new Vector2(
            Mathf.Clamp(error.x * 4f, -maxRates.x, maxRates.x),
            Mathf.Clamp(error.y * 4f, -maxRates.y, maxRates.y));

        switch (mode)
        {
            case FlightControlMode.Angle:
                return level;
            case FlightControlMode.Horizon:
                return Vector2.Lerp(level, acro, Mathf.Max(Mathf.Abs(stick.x), Mathf.Abs(stick.y)));
            case FlightControlMode.AcroTrainer:
                return new Vector2(LimitOutwardRate(acro.x, tilt.x, maxTilt),
                    LimitOutwardRate(acro.y, tilt.y, maxTilt));
            default:
                return acro;
        }
    }

    private static float LimitOutwardRate(float rate, float angle, float limit)
    {
        return Mathf.Clamp(rate, Mathf.Min(0f, (-limit - angle) * 4f),
            Mathf.Max(0f, (limit - angle) * 4f));
    }
}
