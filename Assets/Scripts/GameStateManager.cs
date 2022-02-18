using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GameObject;
using static UnityEngine.Debug;
using static TrackEditor;
/// <summary>
/// Takes care of switching between track editor and play mode
/// </summary>

public class GameStateManager : MonoBehaviour
{
    public static void Init()
    {
        Grid3D.ToggleGridHelper();  // Shows grid
        Grid3D.SetBoundingBox();
        trackEditor.ground.transform.position = new Vector3(0, Grid3D.Bounds["min"].y + Grid3D.CubeSize - .05f, 0);
        trackEditor.ground.SetActive(true);
        selectionCube.SetActive(true);
        SetSelectionCoords(new Coord(1, 1, 1));
        OrbitCamera.Set(selectionCube.transform.position, 50, -30, 200);
        // cameraEditor.SetActive(false);cameraEditor.SetActive(true);  // Something is fucked up, this is a hotfix
    }

    public static void Play()
    {
        EventSystem.current.SetSelectedGameObject(null);

        if (!startPart)
        {
            LogWarning("There is no start!");  // TODO: Show message to user
            return;
        }

        trackEditor.vehiclePrefab.SetActive(false);  // Ensures that MSSceneController is not needed before the vehicle is active (so the vehicle prefab can be active)
        vehicle = Instantiate(trackEditor.vehiclePrefab);
        vehicleRigidBody = vehicle.GetComponent<Rigidbody>();
        vehicleController = Instantiate(trackEditor.vehicleControllerPrefab, Find("UI").transform);
        vehicleController.GetComponent<MSSceneControllerFree>().vehicles[0] = vehicle;
        vehicleController.SetActive(true);
        vehicle.transform.eulerAngles = new Vector3(startPart.transform.eulerAngles.x, startPart.transform.eulerAngles.y + 90, startPart.transform.eulerAngles.z);
        vehicle.transform.position = new Vector3(startPart.transform.position.x, startPart.transform.position.y + .5f, startPart.transform.position.z);
        vehicle.transform.Translate(Vector3.back * 4);
        vehicle.SetActive(true);

        cameraEditor.SetActive(false);
        touchController.enabled = false;
        Grid3D.gridParent.SetActive(false);
        Grid3D.boundingBox.SetActive(false);
        selectionCube.SetActive(false);
        uiTrackEditor.SetActive(false);
        uiGame.SetActive(true);
    }

    public static void Stop()
    {
        Destroy(vehicle);
        Destroy(vehicleController);
        vehicleRigidBody = null;
        Destroy(Find("CameraCar"));  // Because it remains in the scene after car is destroyed >:-[
        cameraEditor.SetActive(true);
        touchController.enabled = true;
        Grid3D.gridParent.SetActive(true);
        Grid3D.boundingBox.SetActive(true);
        selectionCube.SetActive(true);
        uiTrackEditor.SetActive(true);
        uiGame.SetActive(false);
    }
}
