using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Grid holds GridCubes, which holds parts and their position
/// </summary>
public class Grid3D : MonoBehaviour
{
    [SerializeField]
    private GameObject gridCubeHelperPrefab;
    private List<List<List<GridCube>>> _grid = new();  // 3D grid of coordinates
    private GameObject _gridParent;
    
    void Awake()
    {
        Create();
    }
    
    private void Create()
    {
        const int cubeSize = 10;

        // _origin = new Coord(Coord.xCount / 2, Coord.yCount / 2, Coord.zCount / 2);

        _gridParent = new GameObject("gridParent");

        for (int z = 0; z < Coord.zCount; ++z)
        {
            var yCubes = new List<List<GridCube>>();
            for (int y = 0; y < Coord.yCount; ++y)
            {
                var xCubes = new List<GridCube>();
                for (int x = 0; x < Coord.xCount; ++x)
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

    public void Toggle()
    {
        _gridParent.SetActive(!_gridParent.activeSelf);
    }

    /// <summary>
    ///     Gets GridCube at coordinates
    /// </summary>
    /// <param name="coordinates">GridCube coordinates</param>
    /// <returns>GridCube</returns>
    public GridCube GetGridCubeAt(Coord coordinates)
    {
        return _grid[coordinates.x][coordinates.y][coordinates.z];
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
    public Vector3 PositionToGrid(GameObject obj, Coord coordinates)
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
    public Vector3 MoveOnGrid(GameObject part, Coord coordinates)
    {
        // TODO: ► Clear GridCube ar old position
        GetGridCubeAt(coordinates).SetPart(part);
        PositionToGrid(part, coordinates);

        return part.transform.position;
    }
    
    public GameObject GetPartAtCoords(Coord coordinates)
    {
        return GetGridCubeAt(coordinates).part;
    }


}
