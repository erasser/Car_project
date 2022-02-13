using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Debug;
using static UnityEngine.GameObject;
using static UnityEngine.Mathf;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
// using UnityEngine.UI;

// ► There exists Canvas Scaler component
// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    public GameObject partsPrefab;
    [SerializeField] [Tooltip("List of materials assignable to track parts.\nUsed to physically affect the vehicle behavior.\n\nThe first material must be the default one, which prefabs have assigned.")]
    public List<Material> surfaceMaterials;
    [Space]
    [SerializeField] [Tooltip("Wireframe cube visualizer (to show grid lines)")]
    GameObject selectionCubePrefab;
    [SerializeField] [Tooltip("Layer of objects pickable by raycaster (i.e. track parts)")]
    LayerMask selectableObjectsLayer;
    [Space]
    [SerializeField] [Tooltip("This serves to not to be run from the game start, which would cause \"missing MSSceneController\" error.")]
    public GameObject vehicleControllerPrefab;
    [SerializeField]
    public GameObject vehiclePrefab;

    public static TrackEditor trackEditor;
    static Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    public static GameObject ui;
    public static GameObject uiTrackEditor;
    public static GameObject uiGame;
    public static TouchController touchController;
    // static GameObject _ui3D;
    static GameObject _3dUiOverlayRenderTextureImage;

    /*  Editor objects  */
    static readonly List<Transform> PartCategories = new();  // Transform is iterable. Use GetChild(index) to get n-th child.  
    public static readonly List<Transform> Parts = new();  // Transform is iterable. Use GetChild(index) to get n-th child.  
    public static GameObject cameraEditor;
    public static GameObject camera3dUi;
    public GameObject ground;
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
    static Coord _selectionCubeCoords;
    public static GameObject selectedPart;
    static Part _selectedPartComponent;

    void Start()
    {
        trackEditor = this;

        ui = Find("UI");
        uiGame = ui.transform.Find("UI_game").gameObject;
        uiTrackEditor = ui.transform.Find("UI_track_editor").gameObject;
        uiTrackEditor.transform.Find("buttonLoad").GetComponent<Button>().onClick.AddListener(DataManager.Load);
        uiTrackEditor.transform.Find("buttonSave").GetComponent<Button>().onClick.AddListener(DataManager.Save);
        uiTrackEditor.transform.Find("buttonToggleGridHelper").GetComponent<Button>().onClick.AddListener(Grid3D.ToggleGridHelper);
        uiTrackEditor.transform.Find("Go!").GetComponent<Button>().onClick.AddListener(GameStateManager.Play);
        uiGame.transform.Find("Stop").GetComponent<Button>().onClick.AddListener(GameStateManager.Stop);
        uiGame.SetActive(false);

        selectionCube = Instantiate(selectionCubePrefab);
        _selectionCubeMaterial = selectionCube.GetComponent<MeshRenderer>().material;
        SelectionCubeColors.Add("unselected", _selectionCubeMaterial.color);
        SelectionCubeColors.Add("selected", new Color(0, 1, 1, .18f));
        SelectionCubeColors.Add("not allowed", new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors["selected"].a / 2;

        cameraEditor = Find("cameraEditor");
        camera3dUi = Find("Camera_3D_UI_to_render_texture");
        ground = Find("ground");
        ground.SetActive(false);
        track = new GameObject("Track");
        touchController = GetComponent<TouchController>();
        _3dUiOverlayRenderTextureImage = Find("3D_UI_overlay_image");

        Thumbnails.GenerateSurfaceMaterialsThumbnails();
        Thumbnails.GenerateThumbnails();  // Initialization process continues here
        Set3dUiRenderTexture();
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
                
        // var ray = TouchController.cameraUiComponent.ScreenPointToRay(Input.mousePosition);
        // DrawRay(ray.origin, ray.direction * 200, Color.red);
    }

    public static void ApplySurface(byte index)
    {
        // TODO: Store material list in Part.cs list to pick material more effectively?

        if (!selectedPart) return;

        selectedPart.GetComponent<Part>().SetMaterial(index);
    }

    /// <summary>
    ///     Processes selecting / unselecting of track parts.
    /// </summary>
    public void ProcessSimpleTouch()
    {
        if (Physics.Raycast(OrbitCamera.cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
        {
            if (selectionHit.collider.gameObject == selectedPart)
                _selectedPartComponent.Rotate();  // TODO: Check, is transformation can be applied, apply transform & select the part
            else
                SelectPart(selectionHit.collider.gameObject);
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
            selectionHit.collider.gameObject.GetComponent<Part>().Delete();
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Processes 3D UI touch.
    /// </summary>
    /// <param name="selectableUiObjectsLayer">LayerMask, which 3D UI elements have.</param>
    /// <returns>Was something moved?</returns>
    public bool ProcessUiTouch(LayerMask selectableUiObjectsLayer)
    {
        if (!Physics.Raycast(TouchController.cameraUiComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableUiObjectsLayer))
            return false;

        // if (string.CompareOrdinal(selectionHit.collider.name, "centerButton") == 0)  // Fuck me. This is most efficient according to https://cc.davelozinski.com/c-sharp/fastest-way-to-compare-strings
            // TODO: ► Zde jsem skončil. Process center button. + GPU caching
        
        MoveSelection(selectionHit.collider.name);
        return true;
    }

    public static void AddPart(PartSaveData partSaveData)
    {
        // var partFromChildren = _partsCategory0.GetChild(partSaveData.partIndex);
        var partFromChildren = Parts[partSaveData.partIndex];

        if (selectedPart)     // Doesn't apply when a track is loaded
        {
            if (partFromChildren.CompareTag(selectedPart.tag)) return;  // Don't do anything if selected and new part are the same one
            selectedPart.GetComponent<Part>().Delete();                 // Selected part is going to be replaced by the new one
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
            SelectPart(newPart, true);
            coords = _selectionCubeCoords;
        }
        else if (partSaveData.materialIndex != (byte)Part.Surface.Asphalt)  // Loaded Part. Material index = 0 is considered default.
            newPartComponent.SetMaterial(partSaveData.materialIndex);

        newPartComponent.DistributeOverGridCubes(coords);
        newPartComponent.SetRotationForNewPart(partSaveData);
        newPartComponent.MovePartOnGrid(coords);
    }

    void MoveSelection(string arrowName)
    {
        var coords = arrowName switch
        {
            "arrowLeft"  => _selectionCubeCoords.MoveX(-1),
            "arrowRight" => _selectionCubeCoords.MoveX(),
            "arrowDown"  => _selectionCubeCoords.MoveY(-1),
            "arrowUp"    => _selectionCubeCoords.MoveY(),
            "arrowFront" => _selectionCubeCoords.MoveZ(-1),
            "arrowBack"  => _selectionCubeCoords.MoveZ(),
            _ => new Coord()
        };

        if (selectedPart)  // Move selected part if any
            _selectedPartComponent.MovePartOnGrid(coords);

        SetSelectionCoords(coords);
    }

    public static void SetSelectionCoords(Coord coords)
    {
        _selectionCubeCoords = coords;

        // Position selection cube only if it's not attached to a part
        if (!selectionCube.transform.parent)
            Grid3D.PositionToGrid(selectionCube, coords);

        // _camera.transform.LookAt(Grid3D.PositionToGrid(_selectionCube, coords));  // Will be replaced by free camera
    }

    static void SelectPart(GameObject part, bool afterAddPart = false)  // Must reflect UnselectPart()
    {
        if (selectedPart == part) return;

        UnselectPart();

        selectedPart = part;
        _selectedPartComponent = part.GetComponent<Part>();

        _selectedPartComponent.outlineComponent.enabled = true;
        var partDimensions = _selectedPartComponent.gridLocalDimensions;  // Local dimensions, because the selection cube is rotated with the part

        selectionCube.transform.SetParent(part.transform);
        selectionCube.transform.localPosition = Vector3.zero;
        selectionCube.transform.localScale = new Vector3(partDimensions.x * 10 + .1f, 10.1f, partDimensions.z * 10 + .1f);  // parts scale is 2
        _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        _selectionCubeAlphaStartTime = Time.time;

        // Object was selected by touch => set new coordinates for selection cube. Without this, part moves from coordinates of last selected part.
        if (!afterAddPart)
            SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
    }

    public static void UnselectPart()  // Must reflect SelectPart()
    {
        _selectionCubeMaterial.color = SelectionCubeColors["unselected"];

        if (!selectedPart) return;

        selectionCube.transform.parent = null;  // Must be set before SetSelectionCoords()
        SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
        selectedPart = null;
        _selectedPartComponent.outlineComponent.enabled = false;
        _selectedPartComponent = null;

        selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
    }

    /// <summary>
    ///     This is where a part quits editing mode (i.e. current transform is left as is).
    /// </summary>
    static void TryUnselectPart()
    {
        if (!selectedPart) return;

        if (canTransformBeApplied)
        {
            _selectedPartComponent.SaveLastRotation();
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

        canTransformBeApplied = GridCube.AreCubesValid(_selectedPartComponent.occupiedGridCubes);

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
    
    static void Set3dUiRenderTexture()
    {
        var rectTransform = _3dUiOverlayRenderTextureImage.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        rectTransform.transform.position = new(Screen.width / 2f, Screen.height / 2f, 0);
        _3dUiOverlayRenderTextureImage.GetComponent<Image>().material.mainTexture.width = Screen.width;
        _3dUiOverlayRenderTextureImage.GetComponent<Image>().material.mainTexture.height = Screen.height;
    }
}
