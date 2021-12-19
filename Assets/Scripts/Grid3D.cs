using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Grid holds GridCubes, which holds parts and their position
/// </summary>
public class Grid3D : MonoBehaviour
{
    [SerializeField]
    private GameObject gridCubeHelperPrefab;
    private static List<List<List<GridCube>>> _grid = new();  // 3D grid of coordinates
    private static GameObject _gridParent;
    
    void Awake()
    {
        Create();
    }
    
    private void Create()
    {
        const int cubeSize = 10;

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
                    var gridCube = new GridCube(new Vector3(
                        x * cubeSize - Coord.xCount * cubeSize / 2,
                        y * cubeSize - Coord.yCount * cubeSize / 2,
                        z * cubeSize - Coord.zCount * cubeSize / 2));
                    
                    xCubes.Add(gridCube);
                    var cubeHelper = Instantiate(gridCubeHelperPrefab, _gridParent.transform);
                    cubeHelper.transform.SetParent(_gridParent.transform);
                    cubeHelper.transform.position = gridCube.position;
                    cubeHelper.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                }
                yCubes.Add(xCubes);
            }
            _grid.Add(yCubes);
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
        return _grid[coordinates.x][coordinates.y][coordinates.z];
    }
    
    /// <summary>
    ///     Gets n GridCubes from coordinates
    /// </summary>
    /// <param name="coordinates">Initial GridCube coordinates</param>
    /// <param name="axis">Initial GridCube coordinates, can be "x" or "z"</param>
    /// <param name="count">Count of GridCubes to get, negative values are get in opposite direction. Count = 1 returns the initial cube only.</param>
    /// <returns>List of GridCubes</returns>
    public static List<GridCube> GetGridCubes(Coord coordinates, string axis, int count)
    {
        var sign = (int)Mathf.Sign(count);
        var cubes = new List<GridCube>();
        var newCoord = coordinates;
        count = Mathf.Abs(count);

        for (int i = 0; i < count; ++i)
        {
            newCoord.Set(axis, newCoord.Get(axis) + sign * i);
            cubes.Add(GetGridCubeAt(newCoord));
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
        obj.transform.position = GetGridCubeAt(coordinates).position;

        return obj.transform.position;
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
    public static Vector3 MoveOnGrid(GameObject part, Coord coordinates)
    {
        // TODO: ► Clear GridCube ar old position
        GetGridCubeAt(coordinates).SetPart(part);

        return PositionToGrid(part, coordinates);
    }
    
    public static GameObject GetPartAtCoords(Coord coordinates)
    {
        return GetGridCubeAt(coordinates).part;
    }


}
