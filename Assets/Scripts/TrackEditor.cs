using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html

// TODO: Consider using Button (legacy) - Does it cause less draw calls than Button with Text mesh pro? (which is probably, what's used)

public class TrackEditor : MonoBehaviour
{
    [SerializeField]  // prefab
    private GameObject parts;
    [SerializeField]  // prefab
    private GameObject gridCube;
    private Transform _partsCategory0;  // Transform is iterable. Use GetChild(index) to get n-th child.
    private List<List<List<Vector3>>> _grid = new();  // 3D grid of coordinates
    private V3 _origin;  // coordinates of the origin in _grid, i.e. lists indexes of the center cube

    void Start()
    {
        // parts = GameObject.Find("Parts").transform.Find("Category0").transform;  // Now it's a prefab
        GenerateThumbnails();
        GenerateGrid();
    }

    // void FixedUpdate()
    // {}

    void GenerateThumbnails()
    {
        const int thumbSize = 256;  // TODO: Should be relative to screen size
        const int thumbSpacing = 10;  // Total space between two thumbnails
        var rectSize = new Vector2(thumbSize, thumbSize);  // How can I make it const?

        var partsInstance = Instantiate(parts);
        
        _partsCategory0 = partsInstance.transform.Find("Category0").transform;
        
        // Create a render texture for the camera
        var renderTexture = new RenderTexture(thumbSize, thumbSize, 16)
        {
            antiAliasing = 2,
        };

        // Create a camera for shooting parts
        var cameraThumb = new GameObject("cameraThumbnail", typeof(Camera));
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        var i = 0;
        foreach (Transform part in _partsCategory0)
        {
            // Set camera position & look at the part
            cameraThumb.transform.position = part.position + new Vector3(-10, 10, -20);
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
            
            buttonThumb.GetComponent<Button>().onClick.AddListener(PartSelect);

            // imageThumb.AddComponent<EventTrigger>();
            // var eventTrigger = imageThumb.GetComponent<EventTrigger>();

            ++i;
        }

        partsInstance.SetActive(false);
    }
    
    void GenerateGrid()
    {
        const int cubeSize = 10;
        const int xCount = 16;
        const int yCount = 8;
        const int zCount = 10;

        _origin = new V3(xCount / 2, yCount / 2, zCount / 2);  // Assumes the counts are all even

        var gridParent = new GameObject("gridParent");

        for (int z = 0; z < yCount; ++z)
        {
            var yCubes = new List<List<Vector3>>();
            for (int y = 0; y < yCount; ++y)
            {
                var xCubes = new List<Vector3>();
                for (int x = 0; x < xCount; ++x)
                {
                    var coordinates = new Vector3(
                        x * cubeSize - xCount * cubeSize / 2,
                        y * cubeSize - yCount * cubeSize / 2,
                        z * cubeSize - zCount * cubeSize / 2);

                    xCubes.Add(coordinates);

                    var cube = Instantiate(gridCube, gridParent.transform);
                    cube.transform.position = coordinates;
                    cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                }
                yCubes.Add(xCubes);
            }
            _grid.Add(yCubes);
        }
    }

    void PositionToGrid(GameObject obj, V3 position)
    {
        obj.transform.position = _grid[position.x][position.y][position.z];
    }

    void PartSelect()
    {
        var buttonNameParsed = EventSystem.current.currentSelectedGameObject.name.Split('_');
        int partNo = int.Parse(buttonNameParsed[1]);
        var newPart = Instantiate(_partsCategory0.GetChild(partNo)).gameObject;

        PositionToGrid(newPart, _origin);
    }
}
