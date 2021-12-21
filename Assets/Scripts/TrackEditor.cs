using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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
    private GameObject _selectionCube;
    private Coord _selectionCubeCoords;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private Coord _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    private GameObject _camera;
    private GameObject _ground;
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
        
        _selectionCube = Instantiate(selectionCubePrefab);
        _camera = GameObject.Find("CameraEditor");
        _ground = GameObject.Find("ground");
        _ground.SetActive(false);
        GenerateThumbnails();
        _selectionCube.SetActive(true);
        SetSelectionCoords(Coord.zero);
        // _track = new GameObject("Track");
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

        newPart.GetComponent<Part>().MovePartOnGrid(_selectionCubeCoords); 
    }

    void MoveSelection()
    {
        var buttonName = EventSystem.current.currentSelectedGameObject.name;

        if (buttonName == "buttonUp")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveUp());
        }
        else if (buttonName == "buttonDown")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveDown());
        }
        else if (buttonName == "buttonLeft")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveLeft());
        }
        else if (buttonName == "buttonRight")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveRight());
        }
        else if (buttonName == "buttonCloser")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveCloser());
        }
        else if (buttonName == "buttonFarther")
        {
            SetSelectionCoords(_selectionCubeCoords.MoveFarther());
        }
    }

    void SetSelectionCoords(Coord coords)
    {
        _selectionCubeCoords = coords;
        _camera.transform.LookAt(Grid3D.PositionToGrid(_selectionCube, coords));
    }

    void RotatePart()
    {
        var part = Part.GetPartAtCoords(_selectionCubeCoords);

        if (!part) return;
        
        // var buttonName = EventSystem.current.currentSelectedGameObject.name;

        part.GetComponent<Part>().Rotate();
    }
}
