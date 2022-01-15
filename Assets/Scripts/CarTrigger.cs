using UnityEngine;
/// <summary>
///     Changes vehicle behavior based on surface type. Added to vehicle prefab.
/// </summary>

// TODO: ► Change surface parameters only if surface changes

public class CarTrigger : MonoBehaviour
{
    private MSVehicleControllerFree _vehicleController;
    private GameObject _wheelMudParticleEffectLeft;
    private GameObject _wheelMudParticleEffectRight;
    private GameObject _wheelSnowParticleEffectLeft;
    private GameObject _wheelSnowParticleEffectRight;

    private void Start()
    {
        _vehicleController = TrackEditor.vehicle.GetComponent<MSVehicleControllerFree>();

        // TODO: ► Make those Unity editor fields, so they don't need to be searched for.
        _wheelMudParticleEffectLeft   = GameObject.Find("wheelMudParticleEffect");
        _wheelMudParticleEffectRight  = GameObject.Find("wheelMudParticleEffect2");
        _wheelSnowParticleEffectLeft  = GameObject.Find("wheelSnowParticleEffect");
        _wheelSnowParticleEffectRight = GameObject.Find("wheelSnowParticleEffect2");
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

        if (materialFirstChar == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Asphalt].name[0])
        {
            SetPhysicalParams(1.7f, 9);  // TODO: At this point this should be equal to default parameters in Unity editor, because default material & asphalt material are not the same one (but should be)
            DisableParticleEffects();
        } 
        else if (materialFirstChar == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Mud].name[0])
        {
            SetPhysicalParams(.9f, 8);
            SetMudParticleEffect();
        }
        else if (materialFirstChar == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Snow].name[0])
        {
            SetPhysicalParams(0, 7);
            SetSnowParticleEffect();
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
        _wheelMudParticleEffectLeft.SetActive(false);
        _wheelMudParticleEffectRight.SetActive(false);
        _wheelSnowParticleEffectLeft.SetActive(false);
        _wheelSnowParticleEffectRight.SetActive(false);
    }

    void SetMudParticleEffect()
    {
        _wheelMudParticleEffectLeft.SetActive(true);
        _wheelMudParticleEffectRight.SetActive(true);
        _wheelSnowParticleEffectLeft.SetActive(false);
        _wheelSnowParticleEffectRight.SetActive(false);
    }

    void SetSnowParticleEffect()
    {
        _wheelMudParticleEffectLeft.SetActive(false);
        _wheelMudParticleEffectRight.SetActive(false);
        _wheelSnowParticleEffectLeft.SetActive(true);
        _wheelSnowParticleEffectRight.SetActive(true);
    }
}
