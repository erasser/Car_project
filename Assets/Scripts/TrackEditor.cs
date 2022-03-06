using System;
using System.Collections.Generic;
using UnityEngine;
using static UiController;
using static UnityEngine.Debug;
using static UnityEngine.GameObject;
using static UnityEngine.Mathf;

public class TrackEditor : MonoBehaviour
{
    public GameObject partsPrefab;
    [Tooltip("List of materials assignable to track parts.\nUsed to physically affect the vehicle behavior.\n\nThe first material must be the default one, which prefabs have assigned.")]
    public List<Material> surfaceMaterials;
    [Space]
    [Tooltip("Layer of objects pickable by raycaster (i.e. track parts)")]
    public LayerMask selectableObjectsLayer;
    [Space]
    [Tooltip("This serves to not to be run from the game start, which would cause \"missing MSSceneController\" error.")]
    public GameObject vehicleControllerPrefab;
    public GameObject vehiclePrefab;

    public static TrackEditor trackEditor;

    /*  Editor objects  */
    static readonly List<Transform> PartCategories = new();  // Transform is iterable. Use GetChild(index) to get n-th child.  
    public static readonly List<Transform> Parts = new();  // Transform is iterable. Use GetChild(index) to get n-th child.  
    public static GameObject cameraEditor;
    public static GameObject ground;
    public static GameObject track;
    public static GameObject vehicleController;
    public static GameObject vehicle;
    public static Rigidbody vehicleRigidBody;

    /*  Editor states  */      // Must be reset in ResetTrack()
    public static bool canTransformBeApplied;
    public static GameObject startPart;  // Used mainly as a state

    /*  Selection related  */
    public static GameObject selectionCube;
    static readonly Dictionary<String, Color> SelectionCubeColors = new();
    float _selectionCubeAlphaHalf;
    static float _selectionCubeAlphaStartTime;
    static Material _selectionCubeMaterial;
    public static Coord selectionCubeCoords;
    public static Part selectedPart;

    void Start()
    {
        trackEditor = this;

        new UiController();
        selectionCube = Find("SelectionCubeWireframe").gameObject;
        _selectionCubeMaterial = selectionCube.GetComponent<MeshRenderer>().material;
        SelectionCubeColors.Add("unselected", _selectionCubeMaterial.color);
        SelectionCubeColors.Add("selected", new Color(0, 1, 1, .18f));
        SelectionCubeColors.Add("not allowed", new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors["selected"].a / 2;

        cameraEditor = Find("cameraEditor");
        ground = Find("ground");
        ground.SetActive(false);
        track = new GameObject("Track");

        Thumbnails.GenerateSurfaceMaterialsThumbnails();
        Thumbnails.GeneratePartsThumbnails();  // Initialization process continues here

        GameStateManager.Init();
    }

    void Update()
    {
        if (selectedPart)
            _selectionCubeMaterial.color = new Color(
                _selectionCubeMaterial.color.r,
                _selectionCubeMaterial.color.g,
                _selectionCubeMaterial.color.b,
                Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * _selectionCubeAlphaHalf + _selectionCubeAlphaHalf);
                // Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * (_selectionCubeAlphaHalf / 2 - .05f) + _selectionCubeAlphaHalf / 2 + .1f);

        Performance.ShowFPS();
    }

    public static void ApplySurface(byte index)
    {
        // TODO: Store material list in Part.cs list to pick material more effectively?

        if (!selectedPart) return;

        selectedPart.SetMaterial(index);
    }

    /// <summary>
    ///     Processes selecting / unselecting of track parts.
    /// </summary>
    public void ProcessSimpleTouch()
    {
        if (Physics.Raycast(OrbitCamera.cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
        {
            if (selectedPart && selectionHit.collider.gameObject.GetInstanceID() == selectedPart.gameObject.GetInstanceID())
                selectedPart.Rotate();
            else
                SelectPart(selectionHit.collider.GetComponent<Part>());
        }
        else  // Unselect
        {
            TryUnselectPart();
        }
    }

    /// <summary>
    ///     Processes long touch. 
    /// </summary>
    /// <returns>Was something deleted?</returns>
    public bool ProcessHeldTouch()
    {
        if (Physics.Raycast(OrbitCamera.cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
        {
            selectionHit.collider.gameObject.GetComponent<Part>().DeleteSelected();
            return true;
        }
        return false;
    }

    public static void AddPart(PartSaveData partSaveData)
    {
        // var partFromChildren = _partsCategory0.GetChild(partSaveData.partIndex);
        var partFromChildren = Parts[partSaveData.partIndex];

        if (selectedPart)     // Doesn't apply when a track is loaded
        {
            if (partFromChildren.CompareTag(selectedPart.tag)) return;  // Don't do anything if selected and new part are the same one
            selectedPart.DeleteSelected();                 // Selected part is going to be replaced by the new one
        }

        var newPart = Instantiate(partFromChildren, track.transform).gameObject;

        if (newPart.CompareTag("partStart"))
        {
            if (startPart)
            {
                // TODO: Show message to user
                Log("Start already present!");
                return;
            }
            startPart = newPart;
        }

        newPart.transform.localScale = new(2, 2, 2);
        newPart.SetActive(true);
        var newPartComponent = newPart.GetComponent<Part>();
        newPartComponent.partIndex = partSaveData.partIndex;

        var coords = partSaveData.initialOccupiedGridCubeCoord;  // The part is loaded

        if (partSaveData.IsNull()) // The part is chosen by user
        {
            SelectPart(newPartComponent, true);
            coords = selectionCubeCoords;
        }
        else if (partSaveData.materialIndex != (byte)Part.Surface.Asphalt)  // Loaded Part. Material index = 0 is considered default.
            newPartComponent.SetMaterial(partSaveData.materialIndex);

        newPartComponent.DistributeOverGridCubes(coords);
        newPartComponent.SetRotationForNewPart(partSaveData);
        newPartComponent.MovePartOnGrid(coords);
    }

    public static void MoveSelection(string arrowName)
    {
        var coords = arrowName switch
        {
            "arrowLeft"  => selectionCubeCoords.MoveX(-1),
            "arrowRight" => selectionCubeCoords.MoveX(),
            "arrowDown"  => selectionCubeCoords.MoveY(-1),
            "arrowUp"    => selectionCubeCoords.MoveY(),
            "arrowFront" => selectionCubeCoords.MoveZ(-1),
            "arrowBack"  => selectionCubeCoords.MoveZ(),
            _ => new Coord()
        };

        if (selectedPart)  // Move selected part if any
            selectedPart.MovePartOnGrid(coords);

        SetSelectionCoords(coords);
    }

    public static void SetSelectionCoords(Coord coords)
    {
        selectionCubeCoords = coords;

        // Position selection cube only if it's not attached to a part
        if (!selectionCube.transform.parent)
        {
            Grid3D.PositionToGrid(selectionCube, coords);
            Update3dUiTransform();
        }

        // _camera.transform.LookAt(Grid3D.PositionToGrid(_selectionCube, coords));  // Will be replaced by free camera
    }

    public static void SelectPart(Part part, bool afterAddPart = false)  // Must reflect UnselectPart()
    {
        if (selectedPart == part) return;

        UnselectPart();

        selectedPart = part;

        selectedPart.outlineComponent.enabled = true;
        var partDimensions = selectedPart.gridLocalDimensions;  // Local dimensions, because the selection cube is rotated with the part

        selectionCube.transform.SetParent(part.transform);
        selectionCube.transform.localPosition = Vector3.zero;
        selectionCube.transform.localScale = new (partDimensions.x * 10 + .1f, 10.1f, partDimensions.z * 10 + .1f);  // parts scale is 2
        _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        _selectionCubeAlphaStartTime = Time.time;

        // Object was selected by touch => set new coordinates for selection cube. Without this, part moves from coordinates of last selected part.
        if (!afterAddPart)
            SetSelectionCoords(selectedPart.occupiedGridCubes[0].coordinates);
    }

    public static void UnselectPart()  // Must reflect SelectPart()
    {
        if (!selectedPart) return;

        _selectionCubeMaterial.color = SelectionCubeColors["unselected"];

        selectionCube.transform.parent = null;  // Must be set before SetSelectionCoords()
        SetSelectionCoords(selectedPart.occupiedGridCubes[0].coordinates);
        selectedPart.outlineComponent.enabled = false;
        selectedPart = null;

        selectionCube.transform.localScale = new (20.1f, 20.1f, 20.1f);
    }

    /// <summary>
    ///     This is where a part quits editing mode (i.e. current transform is left as is).
    /// </summary>
    public static void TryUnselectPart()
    {
        if (!selectedPart) return;

        if (canTransformBeApplied)
        {
            selectedPart.SaveLastRotation();
            UnselectPart();
        }
        else
        {
            
            // TODO: ► Blink red cube
        }
    }

    /// <summary>
    ///     Must be called after part position or rotation is changed.
    /// </summary>
    public static void UpdateCanTransformBeApplied()
    {
        if (!selectedPart) return;  // The state when parts are being added to the scene, after a track is loaded

        canTransformBeApplied = GridCube.AreCubesValid(selectedPart.occupiedGridCubes);

        UpdateSelectionCubeColor();
    }

    static void UpdateSelectionCubeColor()  // Called when a part is moved or a track is loaded // TODO: Should be called also when part is rotated
    {
        if (!selectedPart)
            _selectionCubeMaterial.color = SelectionCubeColors["unselected"];
        else if (canTransformBeApplied)
            _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        else if (!canTransformBeApplied)
            _selectionCubeMaterial.color = SelectionCubeColors["not allowed"];
    }

    /// <summary>
    ///     Generates a track from loaded track data. 
    /// </summary>
    public static void GenerateLoadedTrack(List<PartSaveData> partsSaveData)
    {
        ResetTrack();

        foreach (var partSaveData in partsSaveData)
            AddPart(partSaveData);

        Part.ClearLastRotation();
    }

    static void ResetTrack()
    {
        foreach (Transform partTransform in track.transform)
            Destroy(partTransform.gameObject);

        Grid3D.Clear();

        if (selectedPart)
        {
            var toDestroy = selectedPart;
            UnselectPart();
            Destroy(toDestroy);
        }

        UpdateCanTransformBeApplied();
        UpdateSelectionCubeColor();

        startPart = null;
    }

    static bool IsValidForPublish()
    {
        // TODO: Check if user finishes the track
        return true;
    }

    void OnApplicationQuit()  // Can't be in GameStateManager, since it's not attached to a gameObject
    {
        vehiclePrefab.SetActive(true);  // So the vehicle prefab remains active in the project
    }
}
