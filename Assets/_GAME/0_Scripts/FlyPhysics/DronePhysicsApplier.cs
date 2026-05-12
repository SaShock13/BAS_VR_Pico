using UnityEngine;

public class DronePhysicsApplier
{
    public void Apply(
        Rigidbody rb,
        DronePhysicsData data)
    {
        rb.mass = data.TotalMass;

        rb.centerOfMass = data.CenterOfMass;

        rb.automaticInertiaTensor = true;
    }
}