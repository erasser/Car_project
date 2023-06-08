using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GameObject;
using static UnityEngine.Debug;
using static TrackEditor;
using static UiController;

/// <summary>
///     Takes care of switching between track editor and play mode
/// </summary>

public class GameStateManager : MonoBehaviour
{
    static GameObject _touchController;

    public static void Init()
    {
        #if UNITY_EDITOR
            _touchController = new GameObject("TouchController", typeof(TouchControllerDesktop));
        #else
            _touchController = new GameObject("TouchController", typeof(TouchControllerTouchScreen));
        #endif

        Grid3D.ToggleGridHelper();  // Shows grid
        Grid3D.SetBoundingBox();
        ground.transform.position = new (0, Grid3D.Bounds["min"].y + Grid3D.CubeSize - .05f, 0);
        ground.SetActive(true);
        Find("selectionHorizontalIndicator").transform.position = ground.transform.position;
        selectionCube.SetActive(true);
        SetSelectionCoords(Grid3D.origin);
        OrbitCamera.Set(selectionCube.transform.position, 50, -30, 300);
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
        vehicleController.transform.Find("Canvas").GetComponent<Canvas>().enabled = true;  // It's the only way to show mobile buttons
        vehicle.transform.eulerAngles = new (startPart.transform.eulerAngles.x, startPart.transform.eulerAngles.y + 90, startPart.transform.eulerAngles.z);
        vehicle.transform.position = new (startPart.transform.position.x, startPart.transform.position.y + .5f, startPart.transform.position.z);
        vehicle.transform.Translate(Vector3.back * 4);
        vehicle.SetActive(true);

        cameraEditor.SetActive(false);
        _touchController.SetActive(false);
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
        _touchController.SetActive(true);
        Grid3D.gridParent.SetActive(true);
        Grid3D.boundingBox.SetActive(true);
        selectionCube.SetActive(true);
        uiTrackEditor.SetActive(true);
        uiGame.SetActive(false);
    }
}
