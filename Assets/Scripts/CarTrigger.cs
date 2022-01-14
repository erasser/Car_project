using UnityEngine;
/// <summary>
///     Changes vehicle behavior based on surface type. Added to vehicle prefab.
/// </summary>

public class CarTrigger : MonoBehaviour
{
    private MSVehicleControllerFree _vehicleController;

    private void Start()
    {
        _vehicleController = TrackEditor.vehicle.GetComponent<MSVehicleControllerFree>();
    }

    //  TODO: Restrict to collide with track parts only
    void OnTriggerEnter(Collider other)  // Collider is a track part, which the surface is picked from.
    {
        var go = other.gameObject;

        if (go.name == "forsageCollider")
            go = go.transform.parent.gameObject;
        
        var materials = go.GetComponent<Renderer>().materials;
        if (materials.Length == 1) return;  // hotfix for ground

        int index;
        index = !other.gameObject.CompareTag("partRoad1") ? 1 : 0;  // hotfix for road1 (has swapped materials)

        // Material is cloned, that's why I compare just the first char
        if (materials[index].name[0] == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Asphalt].name[0])
            SetVehicleParams(1.7f, 9);  // TODO: At this point this should be equal to default parameters in Unity editor, because default material & asphalt material are not the same one (but should be) 
        else if (materials[index].name[0] == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Mud].name[0])
            SetVehicleParams(.9f, 8);
        else if (materials[index].name[0] == TrackEditor.instance.surfaceMaterials[(byte)Part.Surface.Snow].name[0])
            SetVehicleParams(0, 7);
    }

    void SetVehicleParams(float tireSlipsFactor, float engineTorque)
    {
        _vehicleController._vehicleSettings.improveControl.tireSlipsFactor = tireSlipsFactor;
        _vehicleController._vehicleTorque.engineTorque = engineTorque;
    }
}
