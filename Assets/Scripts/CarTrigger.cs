using UnityEngine;
using static UnityEngine.ParticleSystem;

/// <summary>
///     Changes vehicle behavior based on surface type. Added to vehicle prefab.
/// </summary>

// TODO: ► Change surface parameters only if surface changes

public class CarTrigger : MonoBehaviour
{
    private MSVehicleControllerFree _vehicleController;
    private EmissionModule _wheelMudParticleEffectLeft;
    private EmissionModule _wheelMudParticleEffectRight;
    private EmissionModule _wheelSnowParticleEffectLeft;
    private EmissionModule _wheelSnowParticleEffectRight;

    private void Start()
    {
        _vehicleController = TrackEditor.vehicle.GetComponent<MSVehicleControllerFree>();

        // TODO: ► Make those Unity editor fields, so they don't need to be searched for.
        _wheelMudParticleEffectLeft   = GameObject.Find("wheelMudParticleEffect").GetComponent<ParticleSystem>().emission;
        _wheelMudParticleEffectRight  = GameObject.Find("wheelMudParticleEffect2").GetComponent<ParticleSystem>().emission;
        _wheelSnowParticleEffectLeft  = GameObject.Find("wheelSnowParticleEffect").GetComponent<ParticleSystem>().emission;
        _wheelSnowParticleEffectRight = GameObject.Find("wheelSnowParticleEffect2").GetComponent<ParticleSystem>().emission;

        DisableParticleEffects();
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
        _wheelMudParticleEffectLeft.enabled = false;
        _wheelMudParticleEffectRight.enabled = false;
        _wheelSnowParticleEffectLeft.enabled = false;
        _wheelSnowParticleEffectRight.enabled = false;
    }

    void SetMudParticleEffect()
    {
        _wheelMudParticleEffectLeft.enabled = true;
        _wheelMudParticleEffectRight.enabled = true;
        _wheelSnowParticleEffectLeft.enabled = false;
        _wheelSnowParticleEffectRight.enabled = false;
    }

    void SetSnowParticleEffect()
    {
        _wheelMudParticleEffectLeft.enabled = false;
        _wheelMudParticleEffectRight.enabled = false;
        _wheelSnowParticleEffectLeft.enabled = true;
        _wheelSnowParticleEffectRight.enabled = true;
    }
}
