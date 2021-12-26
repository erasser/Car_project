using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Grid holds GridCubes, which holds parts and their position
/// </summary>
public class Grid3D : MonoBehaviour
{
    [SerializeField]
    private GameObject gridCubeHelperPrefab;
    [SerializeField]
    private GameObject boundingBoxPrefab;    // Helper to show grid bounds
    [SerializeField][Tooltip("Width cube count, must be in [3, 255]")]  // TODO: Make better serialized field (spinner?)
    public byte xCount = 10;
    [SerializeField][Tooltip("Height cube count, must be in [3, 255]")]
    public byte yCount = 7;
    [SerializeField][Tooltip("Depth cube count, must be in [3, 255]")]
    public byte zCount = 8;
    const byte CubeSize = 20;

    public static Grid3D instance;
    private static GameObject _boundingBox;
    private static readonly List<List<List<GridCube>>> Grid = new();  // 3D grid of coordinates
    private static GameObject _gridParent;
    // public Dictionary<string, Vector3> bounds = new();

    void Awake()
    {
        instance = this;
        Create();
        SetBoundingBox();
    }
    
    private void Create()
    {
        // _origin = new Coord(Coord.xCount / 2, Coord.yCount / 2, Coord.zCount / 2);

        _gridParent = new GameObject("gridParent");

        for (int x = 0; x < xCount; ++x)
        {
            var yCubes = new List<List<GridCube>>();
            for (int y = 0; y < yCount; ++y)
            {
                var xCubes = new List<GridCube>();
                for (int z = 0; z < zCount; ++z)
                {
                    var gridCube = new GridCube(new (
                        CubeSize * (x  + .5f * (1 - xCount)),
                        CubeSize * (y - yCount * .5f + .5f),
                        CubeSize * (z - zCount * .5f + .5f)),
                        new Coord(x, y, z));

                    xCubes.Add(gridCube);
                    var cubeHelper = Instantiate(gridCubeHelperPrefab, _gridParent.transform);
                    cubeHelper.transform.SetParent(_gridParent.transform);
                    cubeHelper.transform.position = gridCube.position;
                    cubeHelper.transform.localScale = new (CubeSize, CubeSize, CubeSize);
                }
                yCubes.Add(xCubes);
            }
            Grid.Add(yCubes);
        }

        // var bound = new Vector3(-CubeSize * XCount / 2f, -CubeSize * YCount / 2f, -CubeSize * ZCount / 2f);
        // bounds.Add("min", - bound);
        // bounds.Add("max", bound);

        Toggle();
    }

    public static void Toggle()
    {
        _gridParent.SetActive(!_gridParent.activeSelf);
    }

    /// <summary>
    ///     Gets GridCube at coordinates
    /// </summary>
    /// <param name="coordinates">GridCube coordinates</param>
    /// <returns>GridCube</returns>
    public static GridCube GetGridCubeAt(Coord coordinates)
    {
        return Grid[coordinates.x][coordinates.y][coordinates.z];
    }

    /// <summary>
    ///     Gets n GridCubes from coordinates, excluding the initial cube
    /// </summary>
    /// <param name="coordinates">Initial GridCube coordinates</param>
    /// <param name="axis">Initial GridCube coordinates, can be "x" or "z"</param>
    /// <param name="count">Count of GridCubes to get, negative values are get in opposite direction</param>
    /// <param name="excludeInitial">Exclude the initial cube? (i.e. get just the adjacent ones</param>
    /// <returns>List of GridCubes</returns>
    public static List<GridCube> GetGridCubesInLine(Coord coordinates, string axis, int count, bool excludeInitial = false)
    {
        var sign = (int)Mathf.Sign(count);
        var cubes = new List<GridCube>();
        var coord = coordinates;
        count = Mathf.Abs(count);

        for (int i = excludeInitial ? 1 : 0; i < count; ++i)
        {
            coord.Set(axis, coord.Get(axis) + sign * i);
            cubes.Add(GetGridCubeAt(coord));
        }

        return cubes;
    }

    /// <summary>
    ///     Gets GridCubes in countX × countZ area from coordinates 
    /// </summary>
    /// <param name="coordinates">Coordinates of initial cube (which is the closest and leftmost one)</param>
    /// <param name="countX">Count of cubes on X axis</param>
    /// <param name="countZ">Count of cubes on Z axis</param>
    /// <returns>List of GridCubes</returns>
    public static List<GridCube> GetGridCubesInArea(Coord coordinates, int countX, int countZ)
    {
        var cubes = new List<GridCube>();
        var relativeCoordinates = new Coord();

        for (int x = 0; x < countX; ++x)
        {
            relativeCoordinates.x = x;
            for (int z = 0; z < countZ; ++z)
            {
                relativeCoordinates.z = z;
                cubes.Add(GetGridCubeAt(coordinates + relativeCoordinates));
            }
        }

        return cubes;
    }
    
    /// <summary>
    ///     Align object position to GridCube coordinates
    /// </summary>
    /// <para>
    ///     Just sets the position, doesn't update GridCubes
    /// </para>
    /// <param name="obj">Object to align</param>
    /// <param name="coordinates">Target coordinates</param>
    /// <returns>Target position</returns>
    public static Vector3 PositionToGrid(GameObject obj, Coord coordinates)
    {
        return obj.transform.position = GetGridCubeAt(coordinates).position;
    }

    private void SetBoundingBox()
    {
        _boundingBox = Instantiate(boundingBoxPrefab);
        _boundingBox.transform.localScale = new Vector3((xCount - 2) * CubeSize, (yCount - 2) * CubeSize, (zCount - 2) * CubeSize);
        _boundingBox.SetActive(true);

    }
}
