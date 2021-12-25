using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField]
    private LayerMask selectableObjectsLayer;  // Layer of objects pickable by raycaster (i.e. track parts)
    public static TrackEditor instance;
    private static GameObject _selectionCube;
    private static readonly Dictionary<String, Color> SelectionCubeColors = new();
    private float _selectionCubeAlphaHalf;
    private static float _selectionCubeAlphaStartTime;
    private static Material _selectionCubeMaterial;
    private static Coord _selectionCubeCoords;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    private static GameObject _camera;
    private GameObject _ground;
    private static GameObject _selectedPart;
    private static Part _selectedPartComponent;
    private static bool _canTransformBeApplied;

    // private GameObject _track;

    void Start()
    {
        instance = this;

        var ui = GameObject.Find("Canvas");
        ui.transform.Find("buttonUp").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonDown").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonLeft").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonRight").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonCloser").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonFarther").GetComponent<Button>().onClick.AddListener(MoveSelection);
        ui.transform.Find("buttonRotateRight").GetComponent<Button>().onClick.AddListener(RotatePart);
        ui.transform.Find("buttonSelectOrApply").GetComponent<Button>().onClick.AddListener(SelectOrApply);
        
        _selectionCube = Instantiate(selectionCubePrefab);
        _selectionCubeMaterial = _selectionCube.GetComponent<MeshRenderer>().material;
        SelectionCubeColors.Add("unselected", _selectionCubeMaterial.color);
        SelectionCubeColors.Add("selected", new Color(0, 1, 1, .18f));
        SelectionCubeColors.Add("not allowed", new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors["selected"].a / 2;
        _camera = GameObject.Find("cameraEditor");
        _ground = GameObject.Find("ground");
        _ground.SetActive(false);
        GenerateThumbnails();
        _selectionCube.SetActive(true);
        SetSelectionCoords(Coord.zero);
        _camera.SetActive(false);_camera.SetActive(true);  // Something is fucked up, this is a hotfix
        // _track = new GameObject("Track");
    }

    private void Update()
    {
        if (_selectedPart)
            _selectionCubeMaterial.color = new Color(
                _selectionCubeMaterial.color.r,
                _selectionCubeMaterial.color.g,
                _selectionCubeMaterial.color.b,
                Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * _selectionCubeAlphaHalf / 2 + _selectionCubeAlphaHalf / 2);
                // Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * (_selectionCubeAlphaHalf / 2 - .05f) + _selectionCubeAlphaHalf / 2 + .1f);
    }

    public void ProcessSimpleTouch()
    {
        if (Physics.Raycast(OrbitCamera.cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
        {
            if (!_selectedPart)
                SelectPart(selectionHit.collider.gameObject);
            else
            {
                // TODO: Check, is transformation can be applied, apply transform & select the part
                if (selectionHit.collider.gameObject == _selectedPart)
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
    
    void GenerateThumbnails()  // Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        const int thumbSize = 128;   // TODO: Should be relative to screen size
        const int thumbSpacing = 5;  // Total space between two thumbnails
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
    }

    void AddPart()
    {
        var buttonNameParsed = EventSystem.current.currentSelectedGameObject.name.Split('_');
        int partNo = int.Parse(buttonNameParsed[1]);
        var newPart = Instantiate(_partsCategory0.GetChild(partNo)).gameObject;
        newPart.transform.localScale = new(2, 2, 2);
        newPart.SetActive(true);
        SelectPart(newPart, true);  // Must be called before MovePartOnGrid()
        newPart.GetComponent<Part>().MovePartOnGrid(_selectionCubeCoords);
    }

    void MoveSelection()
    {
        var buttonName = EventSystem.current.currentSelectedGameObject.name;
        var coords = new Coord();

        if (buttonName == "buttonUp")
        {
            coords = _selectionCubeCoords.MoveUp();
        }
        else if (buttonName == "buttonDown")
        {
            coords = _selectionCubeCoords.MoveDown();
        }
        else if (buttonName == "buttonLeft")
        {
            coords = _selectionCubeCoords.MoveLeft();
        }
        else if (buttonName == "buttonRight")
        {
            coords = _selectionCubeCoords.MoveRight();
        }
        else if (buttonName == "buttonCloser")
        {
            coords = _selectionCubeCoords.MoveCloser();
        }
        else if (buttonName == "buttonFarther")
        {
            coords = _selectionCubeCoords.MoveFarther();
        }

        if (_selectedPart)  // Move selected part if any
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

    void SelectOrApply()
    {
        // if (selectedPart)  // Transform mode => apply
        //     selectedPart.GetComponent<Part>().ApplyTransform();
        // else     // Selection mode => Try to select
        // {
        //     var part = Part.GetPartAtCoords(_selectionCubeCoords);
        //     if (part)
        //         selectedPart = part;
        // }
    }

    static void SelectPart(GameObject part, bool afterAddPart = false)
    {
        if (_selectedPart == part) return;

        // TODO: UnselectPart()  ??

        _selectedPart = part;
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

    static void UnselectPart()
    {
        if (!_selectedPart) return;

        SetSelectionCoords(_selectedPartComponent.occupiedGridCubes[0].coordinates);
        _selectedPartComponent.outlineComponent.enabled = false;

        _selectedPart = null;
        _selectedPartComponent = null;  // for sure
        _selectionCube.transform.parent = null;
        _selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
        _selectionCubeMaterial.color = SelectionCubeColors["unselected"];
    }

    static void TryUnselectPart()
    {
        if (_selectionCubeMaterial.color != SelectionCubeColors["not allowed"])
            UnselectPart();
        else
        {
            // TODO: ► Blink red cube
        }
    }

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
