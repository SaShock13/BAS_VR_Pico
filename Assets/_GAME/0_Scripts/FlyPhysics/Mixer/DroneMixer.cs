using System.Collections.Generic;
using UnityEngine;

public class DroneMixer
{
    public void Mix(
        IReadOnlyList<DroneMotorRuntime> motors,
        MixerInput input)
    {


        foreach (DroneMotorRuntime motor in motors)
        {
            float output =
                input.Throttle
                + input.PitchCorrection *
                motor.Data.MixData.PitchFactor
                + input.RollCorrection *
                motor.Data.MixData.RollFactor
                + input.YawCorrection *
                motor.Data.MixData.YawFactor;

            output = Mathf.Clamp01(output);

            motor.TargetThrottle = output;


        }
    }
}
