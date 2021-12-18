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
    
    public Vector3 PositionToGrid(GameObject obj, Coord position)
    {
        obj.transform.position = _grid[position.x][position.y][position.z].position;
        return obj.transform.position;
    }

}
