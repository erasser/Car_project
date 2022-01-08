using System;
using UnityEngine;

public class CarTrigger : MonoBehaviour
{
    private MSVehicleControllerFree _vehicleController;

    private void Start()
    {
        _vehicleController = TrackEditor.vehicle.GetComponent<MSVehicleControllerFree>();
    }

    void OnTriggerEnter(Collider other)
    {
        var go = other.gameObject;

        if (go.name == "forsageCollider")
            go = go.transform.parent.gameObject;
        
        var materials = go.GetComponent<Renderer>().materials;
        if (materials.Length == 1) return;  // hotfix for ground

        int index;
        if (!other.gameObject.CompareTag("partRoad1")) // hotfix for road1 (has swapped materials)
            index = 1;
        else
            index = 0;

        if (materials[index].name[0] == TrackEditor.instance.surfaceMaterials[1].name[0])
            _vehicleController._vehicleSettings.improveControl.tireSlipsFactor = 0;
        else
            _vehicleController._vehicleSettings.improveControl.tireSlipsFactor = .85f;
    }
}
