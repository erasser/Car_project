using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

// TODO: ► Svislý stín (vertikální světlo dolů)
// TODO: ► Grid bounding box
// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    private GameObject partsPrefab;
    [SerializeField]
    private GameObject selectionCubePrefab;  // Wireframe cube visualizer (to show grid lines)
    // [SerializeField]
    // private GameObject vehicleControllerPrefab;
    [SerializeField]
    private LayerMask selectableObjectsLayer;  // Layer of objects pickable by raycaster (i.e. track parts)
    public static TrackEditor instance;
    private static GameObject _selectionCube;
    // private static GameObject _vehicleController;
    private static readonly Dictionary<String, Color> SelectionCubeColors = new();
    private float _selectionCubeAlphaHalf;
    private static float _selectionCubeAlphaStartTime;
    private static Material _selectionCubeMaterial;
    private static Coord _selectionCubeCoords;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    private static GameObject _camera;
    private GameObject _ground;
    public static GameObject selectedPart;
    private static Part _selectedPartComponent;
    private static bool _canTransformBeApplied;
    // private GameObject _track;

    void Start()
    {
        instance = this;

        var ui = GameObject.Find("Canvas");
        // ui.transform.Find("buttonUp").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonDown").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonLeft").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonRight").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonCloser").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonFarther").GetComponent<Button>().onClick.AddListener(MoveSelection);
        // ui.transform.Find("buttonRotateRight").GetComponent<Button>().onClick.AddListener(RotatePart);
        ui.transform.Find("Go!").GetComponent<Button>().onClick.AddListener(Play);
        
        _selectionCube = Instantiate(selectionCubePrefab);
        _selectionCubeMaterial = _selectionCube.GetComponent<MeshRenderer>().material;
        SelectionCubeColors.Add("unselected", _selectionCubeMaterial.color);
        SelectionCubeColors.Add("selected", new Color(0, 1, 1, .18f));
        SelectionCubeColors.Add("not allowed", new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors["selected"].a / 2;
        // _vehicleController = Instantiate(vehicleControllerPrefab);
        _camera = GameObject.Find("cameraEditor");
        _ground = GameObject.Find("ground");
        _ground.SetActive(false);
        GenerateThumbnails();  // Initialization process continues here
        // _track = new GameObject("Track");
    }

    private void Update()
    {
        if (selectedPart)
            _selectionCubeMaterial.color = new Color(
                _selectionCubeMaterial.color.r,
                _selectionCubeMaterial.color.g,
                _selectionCubeMaterial.color.b,
                Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * _selectionCubeAlphaHalf + _selectionCubeAlphaHalf);
                // Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * (_selectionCubeAlphaHalf / 2 - .05f) + _selectionCubeAlphaHalf / 2 + .1f);
    }

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

        var partsInstance = Instantiate(partsPrefab);
        
        _partsCategory0 = partsInstance.transform.Find("Category0").transform;
        
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
            buttonThumb.transform.SetParent(GameObject.Find("Canvas").transform);
            buttonThumb.GetComponent<Image>().sprite = sprite;

            // imageThumbImage.transform.position = new Vector3(i * thumbSize + 10, thumbSize + 10, 0);  // also works
            var rectTransform = buttonThumb.GetComponent<RectTransform>();
            rectTransform.transform.position = new (i * (thumbSize + thumbSpacing) + thumbSize * .5f, thumbSize * .5f + thumbSpacing, 0);
            rectTransform.sizeDelta = rectSize;
            // rectTransform.AddComponent<Outline>();  // TODO: Collides with Outline asset

            buttonThumb.GetComponent<Button>().onClick.AddListener(AddPart);

            ++i;
        }

        partsInstance.SetActive(false);
        cameraThumb.SetActive(false);
        _ground.SetActive(true);
        Grid3D.Toggle();  // Shows grid
        Grid3D.SetBoundingBox();
        GameObject.Find("ground").transform.position = new Vector3(0, Grid3D.Bounds["min"].y - .05f, 0);
        _selectionCube.SetActive(true);
        SetSelectionCoords(Coord.zero);
        // OrbitCamera.Set(_selectionCube.transform.position, 50, -30, 200);
        _camera.SetActive(false);_camera.SetActive(true);  // Something is fucked up, this is a hotfix
    }

    void AddPart()
    {
        var buttonNameParsed = EventSystem.current.currentSelectedGameObject.name.Split('_');
        int partNo = int.Parse(buttonNameParsed[1]);
        var partFromChildren = _partsCategory0.GetChild(partNo);

        if (selectedPart)
        {
            if (partFromChildren.CompareTag(selectedPart.tag)) return;  // Don't do anything if selected and new part are the same one
            selectedPart.GetComponent<Part>().Delete();                 // Selected part is going to be replaced by the new one
        }

        var newPart = Instantiate(partFromChildren).gameObject;
        newPart.transform.localScale = new(2, 2, 2);
        newPart.SetActive(true);
        SelectPart(newPart, true);  // Must be called before MovePartOnGrid()
        newPart.GetComponent<Part>().MovePartOnGrid(_selectionCubeCoords);
    }

    void MoveSelection(string arrowName)
    {
        // var buttonName = EventSystem.current.currentSelectedGameObject.name;
        var coords = new Coord();

        if (arrowName == "arrowUp")
        {
            coords = _selectionCubeCoords.MoveUp();
        }
        else if (arrowName == "arrowDown")
        {
            coords = _selectionCubeCoords.MoveDown();
        }
        else if (arrowName == "arrowLeft")
        {
            coords = _selectionCubeCoords.MoveLeft();
        }
        else if (arrowName == "arrowRight")
        {
            coords = _selectionCubeCoords.MoveRight();
        }
        else if (arrowName == "arrowFront")
        {
            coords = _selectionCubeCoords.MoveCloser();
        }
        else if (arrowName == "arrowBack")
        {
            coords = _selectionCubeCoords.MoveFarther();
        }

        if (selectedPart)  // Move selected part if any
            _selectedPartComponent.MovePartOnGrid(coords);

        SetSelectionCoords(coords);
    }

    /// <summary>
    ///     Needed for the state when no part is selected 
    /// </summary>
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
        part.GetComponent<Part>().Rotate();
    }

    void Play()
    {
        var start = GameObject.FindWithTag("partStart");

        if (!start)
        {
            Debug.LogWarning("There is no start!");
            return;
        }

        // GameObject.Find("Vehicle5(drift)").SetActive(true);
        // _vehicleController.SetActive(true);
        // GameObject.Find("Control").GetComponent<MSSceneControllerFree>().enabled = true;
        
        // var vehicle = GameObject.Find("Vehicle3");
        // vehicle.transform.rotation = start.transform.rotation;
        // vehicle.transform.position = new Vector3(start.transform.position.x, start.transform.position.y + 4, start.transform.position.z);

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
        if (!selectedPart) return;

        SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
        _selectedPartComponent.outlineComponent.enabled = false;

        selectedPart = null;
        _selectedPartComponent = null;  // for sure
        _selectionCube.transform.parent = null;
        _selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
        _selectionCubeMaterial.color = SelectionCubeColors["unselected"];
    }

    static void TryUnselectPart()
    {
        if (_canTransformBeApplied)
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
        _canTransformBeApplied = true;

        // Collides with another part?
        foreach (var cube in _selectedPartComponent.occupiedGridCubes)
        {
            if (cube.GetPartsCount() > 1)
                _canTransformBeApplied = false;
        }
        
        // Is out of grid bounds?
        // TODO
        
        UpdateSelectionCubeColor();
    }

    public static void UpdateSelectionCubeColor()  // Called only when part is moved
    {
        // print(canTransformBeApplied);
        if (_canTransformBeApplied && _selectionCubeMaterial.color != SelectionCubeColors["selected"])
            _selectionCubeMaterial.color = SelectionCubeColors["selected"];
        else if (!_canTransformBeApplied && _selectionCubeMaterial.color != SelectionCubeColors["not allowed"])
            _selectionCubeMaterial.color = SelectionCubeColors["not allowed"];
    }
}
