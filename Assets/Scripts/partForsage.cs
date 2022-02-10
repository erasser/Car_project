using UnityEngine;

public class PartForsage : MonoBehaviour
{
    Vector3 _forsageForce;

    void OnTriggerEnter(Collider other)
    {
        _forsageForce = transform.parent.right * 40000;  // It's 'right', because the default parts direction is on x+ axis
    }

    void OnTriggerStay(Collider other)  // Doesn't need fixedDeltaTime, this method is already updated regularly by physics
    {
        TrackEditor.vehicleRigidBody.AddForce(_forsageForce);
    }
}
