using System.Text;
using UnityEngine;

public static class PartInfoText
{
    public static string Title(PartConfig config) => !string.IsNullOrWhiteSpace(config.DisplayName)
        ? config.DisplayName : config.Prefab != null ? config.Prefab.name : config.name;

    public static string Characteristics(PartConfig config)
    {
        var text = new StringBuilder();
        void Line(string label, string value) { if (text.Length > 0) text.AppendLine(); text.Append(label).Append(": ").Append(value); }
        void Number(string label, float value, string unit) { if (value > 0) Line(label, value.ToString("0.###") + " " + unit); }
        Number("Масса", config.Mass * 1000, "г");
        if (config is BatteryConfig battery)
        {
            if (battery.CellCount > 0) Line("Число ячеек", battery.CellCount + "S");
            Number("Ёмкость", battery.CapacityMah, "мА·ч");
            Number("Напряжение", battery.NominalVoltage, "В");
            Number("Макс. ток разряда", battery.MaxDischargeCurrent, "А");
        }
        if (config is MotorConfig motor)
        {
            Number("Макс. тяга", motor.MaxThrust, "Н");
            Number("Макс. обороты", motor.MaxRPM, "об/мин");
            Number("Макс. ток", motor.MaxCurrent, "А");
            Line("Вращение", motor.RotationDirection.ToString());
        }
        if (config is FlightControllerConfig controller)
        {
            Line("Гироскоп", controller.HasGyro ? "Есть" : "Нет");
            Line("Акселерометр", controller.HasAccelerometer ? "Есть" : "Нет");
        }
        if (config is FrameConfig frame && frame.MaxMotors > 0) Line("Макс. число моторов", frame.MaxMotors.ToString());
        if (config.InfoCharacteristics != null)
            foreach (var entry in config.InfoCharacteristics)
                if (!string.IsNullOrWhiteSpace(entry.Label) && !string.IsNullOrWhiteSpace(entry.Value))
                    Line(entry.Label, entry.Value + (string.IsNullOrWhiteSpace(entry.Unit) ? "" : " " + entry.Unit));
        return text.ToString();
    }
}
