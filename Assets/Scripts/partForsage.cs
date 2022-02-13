using UnityEngine;
using static TrackEditor;

public class PartForsage : MonoBehaviour
{
    const float Force = 25000;

    void OnTriggerStay(Collider other)  // Doesn't need fixedDeltaTime, this method is already updated regularly by physics.
    {
        // Causes bigger force, when the car is more aligned with the force part.
        var forceCoefficient = Vector3.Dot(transform.parent.right, vehicleRigidBody.transform.forward);
        if (forceCoefficient < 0)
            forceCoefficient *= .7f;  // for  _direction = vehicleRigidBody.transform.forward;
            // forceCoefficient *= -.8f;  // for  _direction = transform.parent.right (old solution)

        vehicleRigidBody.AddForce(vehicleRigidBody.transform.forward * Force * forceCoefficient);
    }
}
