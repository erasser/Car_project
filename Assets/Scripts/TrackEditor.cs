using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    GameObject partsPrefab;
    [SerializeField] [Tooltip("List of materials assignable to track parts.\nUsed to physically affect the vehicle behavior.\n\nThe first material must be the default one, which prefabs have assigned.")]
    public List<Material> surfaceMaterials;
    [Space]
    [SerializeField] [Tooltip("Wireframe cube visualizer (to show grid lines)")]
    GameObject selectionCubePrefab;
    [SerializeField] [Tooltip("Layer of objects pickable by raycaster (i.e. track parts)")]
    LayerMask selectableObjectsLayer;
    [Space]
    [SerializeField]
    GameObject vehicleControllerPrefab;
    [SerializeField]
    GameObject vehiclePrefab;

    public static TrackEditor instance;
    // private static GameObject _vehicleController;
    static Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    static GameObject _uiTrackEditor;
    static TouchController _touchController;
    static GameObject _ui3D;

    /*  Editor objects  */
    static GameObject _partsInstance;
    static Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    static GameObject _camera;
    static GameObject _cameraVehicle;
    GameObject _ground;
    public static GameObject track;
    static GameObject _vehicleController;
    public static GameObject vehicle;
    public static Rigidbody vehicleRigidBody;

    /*  Editor states  */      // Must be reset in ResetTrack()
    public static bool canTransformBeApplied;
    public static GameObject startPart;  // Used mainly as a state

    /*  Selection related  */
    static GameObject _selectionCube;
    static readonly Dictionary<String, Color> SelectionCubeColors = new();
    float _selectionCubeAlphaHalf;
    static float _selectionCubeAlphaStartTime;
    static Material _selectionCubeMaterial;
    static Coord _selectionCubeCoords;
    public static GameObject selectedPart;
    static Part _selectedPartComponent;

    void Start()
    {
        instance = this;

        _uiTrackEditor = GameObject.Find("UI_track_editor");
        _uiTrackEditor.transform.Find("Go!").GetComponent<Button>().onClick.AddListener(Play);
        _uiTrackEditor.transform.Find("buttonLoad").GetComponent<Button>().onClick.AddListener(DataManager.Load);
        _uiTrackEditor.transform.Find("buttonSave").GetComponent<Button>().onClick.AddListener(DataManager.Save);

        _vehicleController = Instantiate(vehicleControllerPrefab, GameObject.Find("UI").transform);
        vehicle = Instantiate(vehiclePrefab);
        vehicleRigidBody = vehicle.GetComponent<Rigidbody>();

        _selectionCube = Instantiate(selectionCubePrefab);
        _selectionCubeMaterial = _selectionCube.GetComponent<MeshRenderer>().material;
        SelectionCubeColors.Add("unselected", _selectionCubeMaterial.color);
        SelectionCubeColors.Add("selected", new Color(0, 1, 1, .18f));
        SelectionCubeColors.Add("not allowed", new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors["selected"].a / 2;

        _camera = GameObject.Find("cameraEditor");
        _ground = GameObject.Find("ground");
        _ground.SetActive(false);
        track = new GameObject("Track");
        _touchController = GetComponent<TouchController>();
        _ui3D = GameObject.Find("Camera_3D_UI");

        GenerateSurfaceMaterialsThumbnails();
        GenerateThumbnails();  // Initialization process continues here
    }

    void Update()
    {
        if (selectedPart)
            _selectionCubeMaterial.color = new Color(
                _selectionCubeMaterial.color.r,
                _selectionCubeMaterial.color.g,
                _selectionCubeMaterial.color.b,
                Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * _selectionCubeAlphaHalf + _selectionCubeAlphaHalf);
                // Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * (_selectionCubeAlphaHalf / 2 - .05f) + _selectionCubeAlphaHalf / 2 + .1f);
    }

    void GenerateThumbnails()  // Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        const byte thumbSize = 120;   // TODO: Should be relative to screen size
        const float thumbSpacing = 3.5f;  // Total space between two thumbnails
        var rectSize = new Vector2(thumbSize, thumbSize);  // How can I make it const?

        _partsInstance = Instantiate(partsPrefab);
        _partsCategory0 = _partsInstance.transform.Find("Category0").transform;
        
        // Create a render texture for the camera
        var renderTexture = new RenderTexture(thumbSize, thumbSize, 16)
        {
            antiAliasing = 2,
        };

        // Create a camera for shooting partsPrefab
        var cameraThumb = new GameObject("cameraThumbnail", typeof(Camera));
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        byte i = 0;  // TODO: Change to FOR loop
        foreach (Transform part in _partsCategory0)
        {
            // Set camera position & look at the part
            cameraThumb.transform.position = part.position + new Vector3(-4, 8, -15);
            cameraThumb.transform.LookAt(part.position);

            part.gameObject.SetActive(true);  // Show the part for render shot
            RenderTexture.active = cameraThumbCamera.targetTexture;
            cameraThumbCamera.Render();

            // Create a new texture and read the active Render Texture into it.
            var texture = new Texture2D(thumbSize, thumbSize);
            texture.ReadPixels(new Rect(0, 0, thumbSize, thumbSize), 0, 0);
            texture.Apply();

            part.gameObject.SetActive(false);  // Hide the part after shot is taken

            // Create a UI thumbnail (button with image) for each part
            var sprite = Sprite.Create(texture, new Rect(0, 0, thumbSize, thumbSize), Vector2.zero);
            var buttonThumb = new GameObject($"buttonThumb_{i}", typeof(Button), typeof(Image));
            buttonThumb.transform.SetParent(_uiTrackEditor.transform);
            buttonThumb.GetComponent<Image>().sprite = sprite;

            // imageThumbImage.transform.position = new Vector3(i * thumbSize + 10, thumbSize + 10, 0);  // also works
            var rectTransform = buttonThumb.GetComponent<RectTransform>();
            rectTransform.transform.position = new (i * (thumbSize + thumbSpacing) + thumbSize * .5f, thumbSize * .5f + thumbSpacing, 0);
            rectTransform.sizeDelta = rectSize;
            // rectTransform.AddComponent<Outline>();  // TODO: Collides with Outline asset
            byte index = i;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093
            buttonThumb.GetComponent<Button>().onClick.AddListener(delegate {AddPart(new PartSaveData(index, 0, Coord.Null, 0));});

            ++i;
        }

        _partsInstance.SetActive(false);
        cameraThumb.SetActive(false);
        Grid3D.Toggle();  // Shows grid
        Grid3D.SetBoundingBox();
        _ground.transform.position = new Vector3(0, Grid3D.Bounds["min"].y + Grid3D.CubeSize - .05f, 0);
        _ground.SetActive(true);
        _selectionCube.SetActive(true);
        SetSelectionCoords(new Coord(1, 1, 1));
        OrbitCamera.Set(_selectionCube.transform.position, 50, -30, 200);
        _camera.SetActive(false);_camera.SetActive(true);  // Something is fucked up, this is a hotfix
    }

    void GenerateSurfaceMaterialsThumbnails()
    {
        const byte thumbSize = 120;
        const float thumbSpacing = 3.5f;
        var rectSize = new Vector2(thumbSize, thumbSize);
        
        for (byte i = 0; i < surfaceMaterials.Count; ++i)
        {
            var buttonThumb = new GameObject($"buttonSurfaceThumb_{i}", typeof(Button), typeof(Image));
            buttonThumb.transform.SetParent(_uiTrackEditor.transform);
            // buttonThumb.GetComponent<Image>().material = material;

            var rectTransform = buttonThumb.GetComponent<RectTransform>();
            rectTransform.transform.position = new (i * (thumbSize + thumbSpacing) + thumbSize * .5f, thumbSize * 1.5f + 2 * thumbSpacing, 0);
            rectTransform.sizeDelta = rectSize;
            
            byte index = i;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093
            buttonThumb.GetComponent<Button>().onClick.AddListener(delegate {ApplySurface(index);});

            var image = buttonThumb.GetComponent<Image>();
            
            if (i == 0)
                image.color = Color.gray;
            else if (i == 1)
                image.color = new Color(0.43f, 0.19f, 0.06f);
        }
    }

    void ApplySurface(byte index)
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
        if (Physics.Raycast(TouchController.cameraUiComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableUiObjectsLayer))
        {
            MoveSelection(selectionHit.collider.name);
            return true;
        }
        return false;
    }

    static void AddPart(PartSaveData partSaveData)
    {
        var partFromChildren = _partsCategory0.GetChild(partSaveData.partIndex);

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
                Debug.Log("Start already present!");
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

    static void SetSelectionCoords(Coord coords)
    {
        _selectionCubeCoords = coords;

        // Position selection cube only if it's not attached to a part
        if (!_selectionCube.transform.parent)
            Grid3D.PositionToGrid(_selectionCube, coords);

        // _camera.transform.LookAt(Grid3D.PositionToGrid(_selectionCube, coords));  // Will be replaced by free camera
    }

    void Play()
    {
        EventSystem.current.SetSelectedGameObject(null);
    
        if (!vehicle.activeSelf)  // Go ride
        {
            if (!startPart)
            {
                Debug.LogWarning("There is no start!");  // TODO: Show message to user
                return;
            }

            // Funguje i jako restart. Zjistit proč přesně a použít pro Restart()
            _vehicleController.GetComponent<MSSceneControllerFree>().vehicles[0] = vehicle;
            _vehicleController.SetActive(true);
            vehicle.transform.eulerAngles = new Vector3(startPart.transform.eulerAngles.x, startPart.transform.eulerAngles.y + 90, startPart.transform.eulerAngles.z);
            vehicle.transform.position = new Vector3(startPart.transform.position.x, startPart.transform.position.y + .5f, startPart.transform.position.z);
            vehicle.transform.Translate(Vector3.back * 4);
            vehicle.SetActive(true);

            if (!_cameraVehicle)
                _cameraVehicle = GameObject.Find("Camera1");

            _cameraVehicle.SetActive(true);
            _touchController.enabled = false;
            Grid3D.gridParent.SetActive(false);
            _selectionCube.SetActive(false);
            _ui3D.SetActive(false);
        }
        else                       // Stop ride
        {
            vehicle.SetActive(false);
            _vehicleController.SetActive(false);
            _cameraVehicle.SetActive(false);  // Why the fuck is it not attached to the vehicle? (Maybe that's how follow camera works?)
            _touchController.enabled = true;
            Grid3D.gridParent.SetActive(true);
            _selectionCube.SetActive(true);
            _ui3D.SetActive(true);
        }
    }

    static void SelectPart(GameObject part, bool afterAddPart = false)  // Must reflect UnselectPart()
    {
        if (selectedPart == part) return;

        UnselectPart();

        selectedPart = part;
        _selectedPartComponent = part.GetComponent<Part>();

        _selectedPartComponent.outlineComponent.enabled = true;
        var partDimensions = _selectedPartComponent.gridLocalDimensions;  // Local dimensions, because the selection cube is rotated with the part

        _selectionCube.transform.SetParent(part.transform);
        _selectionCube.transform.localPosition = Vector3.zero;
        _selectionCube.transform.localScale = new Vector3(partDimensions.x * 10 + .1f, 10.1f, partDimensions.z * 10 + .1f);  // parts scale is 2
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

        _selectionCube.transform.parent = null;  // Must be set before SetSelectionCoords()
        SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
        selectedPart = null;
        _selectedPartComponent.outlineComponent.enabled = false;
        _selectedPartComponent = null;

        _selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
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
        if (!selectedPart) {
            _selectionCubeMaterial.color = SelectionCubeColors["unselected"];}
        else if (canTransformBeApplied)
        {
            _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        }
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
        {
            AddPart(partSaveData);
        }

        Part.ClearLastRotation();
    }

    static void ResetTrack()
    {
        foreach (Transform partTransform in track.transform)
        {
            Destroy(partTransform.gameObject);
        }

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
}
