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
    private GameObject _selectionCube;
    private static readonly List<Color> SelectionCubeColors = new();
    private float _selectionCubeAlphaHalf;
    private float _selectionCubeAlphaStartTime;
    private static Material _selectionCubeMaterial;
    private Coord _selectionCubeCoords;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    private GameObject _camera;
    private GameObject _ground;
    public static GameObject selectedPart;
    public static bool canTransformBeApplied;
    // private GameObject _track;

    void Start()
    {
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
        SelectionCubeColors.Add(_selectionCubeMaterial.color);            // unselected
        SelectionCubeColors.Add(new Color(0, 1, 1, .18f));     // selected
        SelectionCubeColors.Add(new Color(1, .5f, .5f, .4f));  // apply transform not allowed
        _selectionCubeAlphaHalf = SelectionCubeColors[1].a / 2;
        _camera = GameObject.Find("CameraEditor");
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
        if (selectedPart)
            _selectionCubeMaterial.color = new Color(
                _selectionCubeMaterial.color.r,
                _selectionCubeMaterial.color.g,
                _selectionCubeMaterial.color.b,
                Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * _selectionCubeAlphaHalf / 2 + _selectionCubeAlphaHalf / 2);
                // Mathf.Sin((Time.time - _selectionCubeAlphaStartTime) * 5) * (_selectionCubeAlphaHalf / 2 - .05f) + _selectionCubeAlphaHalf / 2 + .1f);

        // TODO: ► Process only if screen is touched
        ProcessTouch();
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
        SelectPart(newPart);  // Must be called before MovePartOnGrid()
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

        if (selectedPart)  // Move selected part if any
            selectedPart.GetComponent<Part>().MovePartOnGrid(coords);

        SetSelectionCoords(coords);
    }

    void SetSelectionCoords(Coord coords)
    {
        _selectionCubeCoords = coords;
        _camera.transform.LookAt(Grid3D.PositionToGrid(_selectionCube, coords));
    }

    // TODO: Merge with Part.Rotate()?
    void RotatePart()
    {
        var part = Part.GetPartAtCoords(_selectionCubeCoords);

        if (!part) return;
        
        // var buttonName = EventSystem.current.currentSelectedGameObject.name;

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

    void ProcessTouch()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())  // See UFO to implement touch
        {
            if (Physics.Raycast(_camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000, selectableObjectsLayer))
            {
                if (!selectedPart)
                    SelectPart(selectionHit.collider.gameObject);
                else
                {
                    // TODO: Check, is transformation can be applied, apply transform & select the part
                    if (selectionHit.collider.gameObject == selectedPart)
                        selectedPart.GetComponent<Part>().Rotate();
                    else
                        SelectPart(selectionHit.collider.gameObject);
                }
            }
            else  // Unselect
            {
                TryUnselectPart();
            }
        }
    }

    void SelectPart(GameObject part)
    {
        if (selectedPart == part) return;

        // TODO: UnselectPart()  ??

        selectedPart = part;

        var partComponent = selectedPart.GetComponent<Part>();
        partComponent.outlineComponent.enabled = true;
        var partDimensions = partComponent.gridWorldDimensions;

        _selectionCube.transform.SetParent(part.transform);
        _selectionCube.transform.localPosition = Vector3.zero;
        _selectionCube.transform.localScale = new Vector3(partDimensions.x * 10 + .1f, 10.1f, partDimensions.z * 10 + .1f);  // parts scale is 2
        _selectionCubeMaterial.color = SelectionCubeColors[1];
        _selectionCubeAlphaStartTime = Time.time;
    }

    void UnselectPart()
    {
        if (!selectedPart) return;

        var partComponent = selectedPart.GetComponent<Part>();
        SetSelectionCoords(partComponent.occupiedGridCubes[0].coordinates);
        partComponent.outlineComponent.enabled = false;

        selectedPart = null;
        _selectionCube.transform.parent = null;
        _selectionCube.transform.localScale = new Vector3(20.1f, 20.1f, 20.1f);
        _selectionCubeMaterial.color = SelectionCubeColors[0];
    }

    void TryUnselectPart()
    {
        if (_selectionCubeMaterial.color != SelectionCubeColors[2])
            UnselectPart();
        else
        {
            // TODO: Blink red cube
        }
    }

    public static void UpdateCanTransformBeApplied()
    {
        canTransformBeApplied = true;

        // Collides with another part?
        foreach (var cube in selectedPart.GetComponent<Part>().occupiedGridCubes)  // TODO: ► cache this!!!
        {
            if (cube.GetPartsCount() > 1)
                canTransformBeApplied = false;
        }
        
        // Is out of grid bounds?
        // TODO
        
        UpdateSelectionCubeColor();
    }

    public static void UpdateSelectionCubeColor()  // Called only when part is moved
    {
        // print(canTransformBeApplied);
        if (canTransformBeApplied /*&& _selectionCubeMaterial.color != SelectionCubeColors[1]*/)
            _selectionCubeMaterial.color = SelectionCubeColors[1];
        else if (!canTransformBeApplied /*&& _selectionCubeMaterial.color != SelectionCubeColors[2]*/)
            _selectionCubeMaterial.color = SelectionCubeColors[2];
    }
}
