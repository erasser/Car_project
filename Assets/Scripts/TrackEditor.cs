using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

// TODO: ► Svislý stín (vertikální světlo dolů)
// TODO: ► Grid bounding box
// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    private GameObject partsPrefab;
    [SerializeField]
    private GameObject gridCubePrefab;
    [SerializeField]
    private GameObject selectionCubePrefab;
    private GameObject _selectionCube;
    private V3 _selectionCubeCoords;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private List<List<List<Vector3>>> _grid = new();  // 3D grid of coordinates
    private V3 _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube
    private GameObject _camera;

    void Start()
    {
        var ui = GameObject.Find("Canvas");
        ui.transform.Find("buttonUp").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        ui.transform.Find("buttonDown").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        ui.transform.Find("buttonLeft").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        ui.transform.Find("buttonRight").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        ui.transform.Find("buttonCloser").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        ui.transform.Find("buttonFarther").GetComponent<Button>().onClick.AddListener(MoveCameraTarget);
        
        _selectionCube = Instantiate(selectionCubePrefab);
        _camera = GameObject.Find("CameraEditor");
        // _camera.transform.LookAt(Vector3.zero);
        GenerateThumbnails();
        GenerateGrid();
    }

    // void FixedUpdate()
    // {}

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
            cameraThumb.transform.position = part.position + new Vector3(-8, 8, -15);
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
            rectTransform.transform.position = new Vector3(i * (thumbSize + thumbSpacing) + thumbSize * .5f, thumbSize * .5f + thumbSpacing, 0);
            rectTransform.sizeDelta = rectSize;
            
            buttonThumb.GetComponent<Button>().onClick.AddListener(AddPart);

            ++i;
        }

        partsInstance.SetActive(false);
        cameraThumb.SetActive(false);
    }
    
    void GenerateGrid()
    {
        const int cubeSize = 10;

        _origin = new V3(V3.xCount / 2, V3.yCount / 2, V3.zCount / 2);

        var gridParent = new GameObject("gridParent");

        for (int z = 0; z < V3.zCount; ++z)
        {
            var yCubes = new List<List<Vector3>>();
            for (int y = 0; y < V3.yCount; ++y)
            {
                var xCubes = new List<Vector3>();
                for (int x = 0; x < V3.xCount; ++x)
                {
                    var coordinates = new Vector3(
                        x * cubeSize - V3.xCount * cubeSize / 2,
                        y * cubeSize - V3.yCount * cubeSize / 2,
                        z * cubeSize - V3.zCount * cubeSize / 2);

                    xCubes.Add(coordinates);

                    var cube = Instantiate(gridCubePrefab, gridParent.transform);
                    cube.transform.position = coordinates;
                    cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                }
                yCubes.Add(xCubes);
            }
            _grid.Add(yCubes);
        }

        _selectionCube.SetActive(true);
        SetSelectionCoords(V3.zero);
    }

    Vector3 PositionToGrid(GameObject obj, V3 position)
    {
        obj.transform.position = _grid[position.x][position.y][position.z];
        return obj.transform.position;
    }

    void AddPart()
    {
        var buttonNameParsed = EventSystem.current.currentSelectedGameObject.name.Split('_');
        int partNo = int.Parse(buttonNameParsed[1]);
        var newPart = Instantiate(_partsCategory0.GetChild(partNo)).gameObject;
        newPart.SetActive(true);

        PositionToGrid(newPart, _selectionCubeCoords);
    }

    void MoveCameraTarget()
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

    void SetSelectionCoords(V3 coords)
    {
        _selectionCubeCoords = coords;
        var tmp = PositionToGrid(_selectionCube, coords);
        print(tmp);
        _camera.transform.LookAt(tmp);
    }
}
