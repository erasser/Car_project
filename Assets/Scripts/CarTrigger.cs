using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.ParticleSystem;

/// <summary>
///     Changes vehicle behavior based on surface type. Added to vehicle prefab.
/// </summary>

// TODO: ► Change surface parameters only if surface changes

public class CarTrigger : MonoBehaviour
{
    Rigidbody _rigidBody;
    MSVehicleControllerFree _vehicleController;
    EmissionModule _wheelMudParticleEffectLeftEmission;
    EmissionModule _wheelMudParticleEffectRightEmission;
    EmissionModule _wheelSnowParticleEffectLeftEmission;
    EmissionModule _wheelSnowParticleEffectRightEmission;
    float _noTriggerTimeSum;  // Causes wheel particle effect persistence
    // bool _isOnGround; // That's 2nd option beside _noTriggerTimeSum, it's without wheel particle effect persistence after car is sent to fly. It's LITTLE bit more performant, since it doesn't increment time.

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _vehicleController = TrackEditor.vehicle.GetComponent<MSVehicleControllerFree>();

        // TODO: ► Make those Unity editor fields, so they don't need to be searched for.
        _wheelMudParticleEffectLeftEmission   = Find("wheelMudParticleEffect").GetComponent<ParticleSystem>().emission;
        _wheelMudParticleEffectRightEmission  = Find("wheelMudParticleEffect2").GetComponent<ParticleSystem>().emission;
        _wheelSnowParticleEffectLeftEmission  = Find("wheelSnowParticleEffect").GetComponent<ParticleSystem>().emission;
        _wheelSnowParticleEffectRightEmission = Find("wheelSnowParticleEffect2").GetComponent<ParticleSystem>().emission;

        DisableParticleEffects();
    }

    void FixedUpdate()
    {
        _noTriggerTimeSum += Time.fixedDeltaTime;

        /*  Disable wheels particle effect when car if flying  */
        if (_noTriggerTimeSum > .2f)    // if (!_isOnGround) - another option, see declaration comment
            DisableParticleEffects();

        /*  Adjusts wheels particle effects according to car speed */
        if (_wheelMudParticleEffectLeftEmission.enabled)  // TODO: Implement wheelie (when car is stationary)
        {
            var rate = _rigidBody.velocity.magnitude * 1.1f + _rigidBody.angularVelocity.magnitude * 20;

            _wheelMudParticleEffectLeftEmission.rateOverTime = rate;
            _wheelMudParticleEffectRightEmission.rateOverTime = rate;
        }
        else if (_wheelSnowParticleEffectLeftEmission.enabled)
        {
            var rate = _rigidBody.velocity.magnitude * 1.1f + _rigidBody.angularVelocity.magnitude * 20;

            _wheelSnowParticleEffectLeftEmission.rateOverTime = rate;
            _wheelSnowParticleEffectRightEmission.rateOverTime = rate;

            // It does pretty nothing
            // var shape = _wheelSnowParticleEffectLeft.shape;
            // shape.rotation = new Vector3(-45 + magnitude * 3, 0, 0);
            // shape = _wheelSnowParticleEffectRight.shape;
            // shape.rotation = new Vector3(-45 + magnitude * 3, 0, 0);
        }
    }

    //  TODO: Restrict to collide with track parts only
    void OnTriggerEnter(Collider other)  // Collider is a track part, which the surface is picked from.
    {
        var go = other.gameObject;

        if (go.name == "forsageCollider")
            go = go.transform.parent.gameObject;

        var materials = go.GetComponent<Renderer>().materials;
        if (materials.Length == 1) return;  // hotfix for ground

        // Material is cloned, that's why I compare just the first char
        var materialFirstChar = materials[other.gameObject.CompareTag("partRoad1") ? 0 : 1].name[0];  // hotfix for road1 (has swapped materials)

        if (materialFirstChar == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Mud].name[0])
        {
            SetPhysicalParams(.9f, 8);
            SetMudParticleEffect();
        }
        else if (materialFirstChar == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Snow].name[0])
        {
            SetPhysicalParams(0, 7);
            SetSnowParticleEffect();
        }
        else  // Default = asphalt surface
        {
            SetPhysicalParams(1.7f, 9);
            DisableParticleEffects();
        } 
    }

    void OnTriggerStay(Collider other)
    {
        _noTriggerTimeSum = 0;    // _isOnGround = true; - another option
    }

    /*void OnTriggerExit(Collider other)  // Beware: This is triggered right AFTER OnTriggerEnter()
    {
        // _isOnGround = false;    // another option
    }*/

    /// <summary>
    ///     Sets all needed parameters based on surface characteristics
    /// </summary>
    /// <param name="tireSlipsFactor">Bigger value results in bigger grip.</param>
    /// <param name="engineTorque">Acceleration (as I believe to).</param>
    void SetPhysicalParams(float tireSlipsFactor, float engineTorque)
    {
        _vehicleController._vehicleSettings.improveControl.tireSlipsFactor = tireSlipsFactor;
        _vehicleController._vehicleTorque.engineTorque = engineTorque;
    }

    void DisableParticleEffects()
    {
        _wheelMudParticleEffectLeftEmission.enabled = false;
        _wheelMudParticleEffectRightEmission.enabled = false;
        _wheelSnowParticleEffectLeftEmission.enabled = false;
        _wheelSnowParticleEffectRightEmission.enabled = false;
    }

    void SetMudParticleEffect()
    {
        _wheelMudParticleEffectLeftEmission.enabled = true;
        _wheelMudParticleEffectRightEmission.enabled = true;
        _wheelSnowParticleEffectLeftEmission.enabled = false;
        _wheelSnowParticleEffectRightEmission.enabled = false;
    }

    void SetSnowParticleEffect()
    {
        _wheelMudParticleEffectLeftEmission.enabled = false;
        _wheelMudParticleEffectRightEmission.enabled = false;
        _wheelSnowParticleEffectLeftEmission.enabled = true;
        _wheelSnowParticleEffectRightEmission.enabled = true;
    }
}
