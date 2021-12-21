using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Grid holds GridCubes, which holds parts and their position
/// </summary>
public class Grid3D : MonoBehaviour
{
    [SerializeField]
    private GameObject gridCubeHelperPrefab;
    private static readonly List<List<List<GridCube>>> Grid = new();  // 3D grid of coordinates
    private static GameObject _gridParent;
    
    void Awake()
    {
        Create();
    }
    
    private void Create()
    {
        const int cubeSize = 20;

        // _origin = new Coord(Coord.xCount / 2, Coord.yCount / 2, Coord.zCount / 2);

        _gridParent = new GameObject("gridParent");

        for (int x = 0; x < Coord.xCount; ++x)
        {
            var yCubes = new List<List<GridCube>>();
            for (int y = 0; y < Coord.yCount; ++y)
            {
                var xCubes = new List<GridCube>();
                for (int z = 0; z < Coord.zCount; ++z)
                {
                    var gridCube = new GridCube(new (
                        x * cubeSize - Coord.xCount * cubeSize / 2,
                        y * cubeSize - Coord.yCount * cubeSize / 2,
                        z * cubeSize - Coord.zCount * cubeSize / 2), new Coord(x, y, z));

                    xCubes.Add(gridCube);
                    var cubeHelper = Instantiate(gridCubeHelperPrefab, _gridParent.transform);
                    cubeHelper.transform.SetParent(_gridParent.transform);
                    cubeHelper.transform.position = gridCube.position;
                    cubeHelper.transform.localScale = new (cubeSize, cubeSize, cubeSize);
                }
                yCubes.Add(xCubes);
            }
            Grid.Add(yCubes);
        }
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
    /// <param name="countZ">Count of cubes on X axis</param>
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

    /// <summary>
    ///     Moves track part on grid to coordinates
    /// </summary>
    /// <para>
    ///     Also handles updating of GridCubes
    /// </para>
    /// <param name="part">Part to move</param>
    /// <param name="coordinates">Target coordinates</param>
    /// <returns>Target position</returns>
    public static Vector3 MovePartOnGrid(GameObject part, Coord coordinates)  // TODO: Move to Part.cs?
    {
        // TODO: ► Clear GridCube at old position

        // var cubes = GetGridCubesInLine(coordinates, "x", part.GetComponent<Part>().gridWorldDimensions.x);  // adjacent cubes on x axis
        // cubes.AddRange(GetGridCubesInLine(coordinates, "z", part.GetComponent<Part>().gridWorldDimensions.z));  // adjacent cubes on z axis
        // cubes.Add(GetGridCubeAt(coordinates));  // initial cube

        // Distribute the part over GridCubes
        var cubes = GetGridCubesInArea(coordinates, part.GetComponent<Part>().gridWorldDimensions.x, part.GetComponent<Part>().gridWorldDimensions.z);
        foreach (var cube in cubes)
        {
            cube.SetPart(part);
        }

        return part.GetComponent<Part>().PositionPart(cubes);
    }
    
    public static GameObject GetPartAtCoords(Coord coordinates)  // TODO: Move to Part.cs?
    {
        return GetGridCubeAt(coordinates).part;
    }


}
