using System;
using System.Collections.Generic;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

// TODO: ► Svislý stín (vertikální světlo dolů)
// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    GameObject partsPrefab;
    [SerializeField]
    GameObject selectionCubePrefab;  // Wireframe cube visualizer (to show grid lines)
    [SerializeField]
    LayerMask selectableObjectsLayer;  // Layer of objects pickable by raycaster (i.e. track parts)

    [SerializeField]
    GameObject vehicleControllerPrefab;
    [SerializeField]
    GameObject vehiclePrefab;


    public static TrackEditor instance;
    // private static GameObject _vehicleController;
    Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    GameObject _uiTrackEditor;

    /*  Editor objects  */
    private static GameObject _partsInstance;
    static Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    static GameObject _camera;
    static GameObject _cameraVehicle;
    GameObject _ground;
    public static GameObject track;
    GameObject _vehicleController;
    GameObject _vehicle;

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
        _vehicle = Instantiate(vehiclePrefab);
        
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

    /// <summary>
    ///     Processes selecting / unselecting of track parts.
    /// </summary>
    public void ProcessSimpleTouch()
    {
        if (Physics.Raycast(OrbitCamera.cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
        {
            if (!selectedPart)
                SelectPart(selectionHit.collider.gameObject);
            else
            {
                // TODO: Check, is transformation can be applied, apply transform & select the part
                if (selectionHit.collider.gameObject == selectedPart)
                    _selectedPartComponent.Rotate();
                else
                    SelectPart(selectionHit.collider.gameObject);
            }
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

        var i = 0;
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
            int index = i;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093
            buttonThumb.GetComponent<Button>().onClick.AddListener(delegate {AddPart(index, Coord.Null);});

            ++i;
        }

        _partsInstance.SetActive(false);
        cameraThumb.SetActive(false);
        _ground.SetActive(true);
        Grid3D.Toggle();  // Shows grid
        Grid3D.SetBoundingBox();
        GameObject.Find("ground").transform.position = new Vector3(0, Grid3D.Bounds["min"].y - .05f, 0);
        _selectionCube.SetActive(true);
        SetSelectionCoords(new Coord(1, 1, 1));
        OrbitCamera.Set(_selectionCube.transform.position, 50, -30, 200);
        _camera.SetActive(false);_camera.SetActive(true);  // Something is fucked up, this is a hotfix
    }

    static void AddPart(int partIndex, Coord coords)
    {
        var partFromChildren = _partsCategory0.GetChild(partIndex);

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
        var partComponent = newPart.GetComponent<Part>();
        partComponent.partIndex = partIndex;

        if (coords.IsNull())  // The part is chosen by user
        {
            SelectPart(newPart, true);  // Must be called before MovePartOnGrid()
            coords = _selectionCubeCoords;
        }

        partComponent.MovePartOnGrid(coords);
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

    // TODO: Merge with Part.Rotate()?
    void RotatePart()
    {
        // old code...
        var part = Part.GetPartAtCoords(_selectionCubeCoords);

        if (!part) return;
        
        // old code...
        part.Rotate();
    }

    void Play()
    {
        if (!_vehicle.activeSelf)  // Go ride
        {
            if (!startPart)
            {
                Debug.LogWarning("There is no start!");  // TODO: Show message to user
                return;
            }

            // Funguje i jako restart. Zjistit proč přesně a použít pro Restart()
            _vehicleController.GetComponent<MSSceneControllerFree>().vehicles[0] = _vehicle;
            _vehicleController.SetActive(true);
            _vehicle.transform.eulerAngles = new Vector3(startPart.transform.eulerAngles.x, startPart.transform.eulerAngles.y + 90, startPart.transform.eulerAngles.z);
            _vehicle.transform.position = new Vector3(startPart.transform.position.x, startPart.transform.position.y + 2, startPart.transform.position.z);
            _vehicle.SetActive(true);

            if (!_cameraVehicle)
                _cameraVehicle = GameObject.Find("Camera1");

            _cameraVehicle.SetActive(true);
        }
        else                       // Stop ride
        {
            _vehicle.SetActive(false);
            _vehicleController.SetActive(false);
            _cameraVehicle.SetActive(false);  // Why the fuck is it not attached to the vehicle?
        }
    }

    static void SelectPart(GameObject part, bool afterAddPart = false)  // Must reflect UnselectPart()
    {
        if (selectedPart == part) return;

        UnselectPart();

        selectedPart = part;
        _selectedPartComponent = part.GetComponent<Part>();

        _selectedPartComponent.outlineComponent.enabled = true;
        var partDimensions = _selectedPartComponent.gridWorldDimensions;

        _selectionCube.transform.SetParent(part.transform);
        _selectionCube.transform.localPosition = Vector3.zero;
        _selectionCube.transform.localScale = new Vector3(partDimensions.x * 10 + .1f, 10.1f, partDimensions.z * 10 + .1f);  // parts scale is 2
        _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        _selectionCubeAlphaStartTime = Time.time;

        if (!afterAddPart)  // Object was selected by touch => set new coordinates for selection cube.
            SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
    }

    public static void UnselectPart()  // Must reflect SelectPart()
    {
        _selectionCubeMaterial.color = SelectionCubeColors["unselected"];

        if (!selectedPart) return;

        SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
        _selectedPartComponent.outlineComponent.enabled = false;

        selectedPart = null;
        _selectedPartComponent = null;
        _selectionCube.transform.parent = null;
        _selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
    }

    static void TryUnselectPart()
    {
        if (canTransformBeApplied)
            UnselectPart();
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

    // TODO: ► Set rotation
    /// <summary>
    ///     Generates a track from loaded track data. 
    /// </summary>
    public static void GenerateLoadedTrack(List<PartSaveData> partsSaveData)
    {
        ResetTrack();

        foreach (var partSaveData in partsSaveData)
            AddPart(partSaveData.partIndex, partSaveData.initialOccupiedGridCubeCoord);
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
