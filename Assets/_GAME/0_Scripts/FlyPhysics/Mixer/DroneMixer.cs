using System.Collections.Generic;
using UnityEngine;

public class DroneMixer
{
    public void Mix(
        IReadOnlyList<DroneMotorRuntime> motors,
        MixerInput input)
    {


        Debug.Log($"******Input throttle - {input.Throttle} RollCorrection - {input.RollCorrection} YawCorrection - {input.YawCorrection} PitchCorrection - {input.PitchCorrection} ");
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


            Debug.Log($"********motor  output {output}");
        }
    }
}