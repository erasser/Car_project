using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using static TrackEditor;

/// <summary>
///     Grid holds GridCubes, which holds parts and their position
/// </summary>
public class Grid3D : MonoBehaviour
{
    public GameObject gridCubeHelperPrefab;
    public GameObject boundingBoxPrefab;    // Helper to show grid bounds
    [Space]  // TODO: Make better serialized field (spinner?)
    [Tooltip("Width cube count, must be in [3, 255]")]
    public byte xCount = 10;
    [Tooltip("Height cube count, must be in [3, 255]")]
    public byte yCount = 7;
    [Tooltip("Depth cube count, must be in [3, 255]")]
    public byte zCount = 8;
    public const byte CubeSize = 20;
    public static Coord origin;  // coordinates of the origin in the grid, i.e. lists indexes of the center cube

    public static Grid3D instance;
    public static GameObject boundingBox;
    static readonly List<List<List<GridCube>>> Grid = new();  // 3D grid of coordinates
    public static GameObject gridParent;
    public static readonly Dictionary<string, Vector3> Bounds = new();

    void Awake()
    {
        instance = this;
        Create();
    }

    void Create()
    {
        origin = new Coord(xCount / 2, 1, zCount / 2);

        gridParent = new GameObject("gridParent");

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
                    var cubeHelper = Instantiate(gridCubeHelperPrefab, gridParent.transform);
                    cubeHelper.transform.SetParent(gridParent.transform);
                    cubeHelper.transform.position = gridCube.position;
                    cubeHelper.transform.localScale = new (CubeSize, CubeSize, CubeSize);
                }
                yCubes.Add(xCubes);
            }
            Grid.Add(yCubes);
        }

        var bound = new Vector3(CubeSize * instance.xCount / 2f, CubeSize * instance.yCount / 2f, CubeSize * instance.zCount / 2f);
        Bounds.Add("min", - bound);
        Bounds.Add("max", bound);

        OrbitCamera.SetTargetPositionLimits(Bounds["min"], Bounds["max"]);

        // ToggleGridHelper();
    }

    public static void ToggleGridHelper(/*bool enable*/)
    {
        gridParent.SetActive(!gridParent.activeSelf);
        // gridParent.SetActive(enable);
    }

    // public static void ToggleBoundingHelper(bool enable)
    // {
        // _boundingBox.SetActive(!_boundingBox.activeSelf);
    //     _boundingBox.SetActive(enable);
    // }

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
        var sign = (int)Sign(count);
        var cubes = new List<GridCube>();
        var coord = coordinates;
        count = Abs(count);

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

    public static void SetBoundingBox()
    {
        boundingBox = Instantiate(instance.boundingBoxPrefab);
        boundingBox.transform.localScale = new ((instance.xCount - 2) * CubeSize, (instance.yCount - 2) * CubeSize, (instance.zCount - 2) * CubeSize);
        boundingBox.SetActive(true);
    }

    public static void Clear()
    {
        foreach (var rowX in Grid)
            foreach (var rowY in rowX)
                foreach (var cube in rowY)
                    cube.Clear();
    }

    /// <summary>  // If this is used, try to use recursion \m/
    ///     Checks if cubes are valid
    /// </summary>
    // public static bool IsValid()
    // {
    //     foreach (var rowX in Grid)
    //     {
    //         foreach (var rowY in rowX)
    //         {
    //             foreach (var cube in rowY)
    //             {
    //                 if (!cube.IsValid())
    //                     return false;
    //             }
    //         }
    //     }
    //     return true;
    // }

    // public static List<GridCubeSaveData> GetOccupiedCubesSaveData()
    // {
    //     var cubesSaveData = new List<GridCubeSaveData>();
    //
    //     foreach (var rowX in Grid)
    //     {
    //         foreach (var rowY in rowX)
    //         {
    //             foreach (var cube in rowY)
    //             {
    //                 if (cube.ShouldBeSaved())
    //                     cubesSaveData.Add(cube.GetGridCubeSaveData());
    //             }
    //         }
    //     }
    //     return cubesSaveData;
    // }
}
