using System.Collections.Generic;
using UnityEngine;

public class DroneMixer
{
    public void Mix(
        IReadOnlyList<DroneMotorRuntime> motors,
        FlightInput input)
    {


        Debug.Log($"******Input throttle - {input.Throttle} roll - {input.Roll} YAw - {input.Yaw} Pitch - {input.Pitch} ");
        foreach (DroneMotorRuntime motor in motors)
        {
            float output =
                input.Throttle
                + input.Pitch *
                motor.Data.MixData.PitchFactor
                + input.Roll *
                motor.Data.MixData.RollFactor
                + input.Yaw *
                motor.Data.MixData.YawFactor;

            output = Mathf.Clamp01(output);

            motor.TargetThrottle = output;


            Debug.Log($"********motor  PitchFactor {motor.Data.MixData.PitchFactor}");
        }
    }
}