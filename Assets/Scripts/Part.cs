using System.Collections.Generic;
using UnityEngine;

// Všechny GridCube, přes které part zabírá místo, budou referencovat ten part

public class Part : MonoBehaviour
{
    // public GridCube gridCube;           // The main GridCube, at which the part is held
    private Coord _gridLocalDimensions; // Count of GridCubes, that the part is going to occupy in local space, calculated just once
    public Coord gridWorldDimensions;   // Count of GridCubes, that the part is going to occupy in world space (but with local position), updated on rotation
    public List<GridCube> occupiedGridCubes = new();    // List of all GridCubes the part is occupying, including the main gridCube ↑
    private byte _rotation;             // 0, 1, 2, 3

    private void Awake()
    {
        CalculateLocalCubeDimensions();
    }

    public void Rotate()
    {
        print("rotate!");
        transform.eulerAngles = new (0, ++_rotation * 90, 0);  // Rotates right
        UpdateWorldCubeDimensions();

        // TODO: Check, if rotation is possible
        // For ■■ shape
        if (gridWorldDimensions.x == 1 && gridWorldDimensions.z == 2) // from _ to |
        {
            Grid3D.GetGridCubeAt(new Coord(occupiedGridCubes[0].coordinates.x, occupiedGridCubes[0].coordinates.y, occupiedGridCubes[0].coordinates.z + 1)).SetPart(gameObject);
            occupiedGridCubes[1].UnsetPart(gameObject);
        }
        else if (gridWorldDimensions.x == 2 && gridWorldDimensions.z == 1) // from | to _
        {
            
        }
    }

    /// <summary>
    ///     Calculates GridCubes count for each axis based on mesh dimensions
    /// </summary>
    private void CalculateLocalCubeDimensions()
    {
        var meshSize = GetComponent<MeshFilter>().sharedMesh.bounds.size;

        _gridLocalDimensions = new Coord(GetCubesCount(meshSize.x), GetCubesCount(meshSize.y), GetCubesCount(meshSize.z));

        int GetCubesCount(float meshDimension)
        {
            // Subtract .1 to fix model vertices position inaccuracy
            // 10 units is the edge size of one cube
            // Must be at least 1
            return Mathf.Max(Mathf.CeilToInt((meshDimension - .1f) / 10), 1);
        }
        
        UpdateWorldCubeDimensions();
    }

    // List<GridCube> UpdateOccupiedGridCubes()
    // {
    //     var relativeCoordinates = new Coord();
    //
    //     for (int x = 0; x < countX; ++x)
    //     {
    //         relativeCoordinates.x = x;
    //         for (int z = 0; z < countZ; ++z)
    //         {
    //             relativeCoordinates.z = z;
    //             cubes.Add(GetGridCubeAt(coordinates + relativeCoordinates));
    //         }
    //     }
    //
    //
    //     return occupiedGridCubes;
    // }

    private void UpdateWorldCubeDimensions()
    {
        if (_rotation is 0 or 2) // :-o
            gridWorldDimensions = _gridLocalDimensions;
        else
        {
            gridWorldDimensions.x = _gridLocalDimensions.z;
            gridWorldDimensions.z = _gridLocalDimensions.x;
        }
    }

    public Vector3 PositionPart(List<GridCube> gridCubes)
    {
        occupiedGridCubes = gridCubes;
        return transform.position = new(
            (gridCubes[^1].position.x + gridCubes[0].position.x) / 2,
            gridCubes[0].position.y,
            (gridCubes[^1].position.z + gridCubes[0].position.z) / 2);
    }
    
    /// <summary>
    ///     Moves track part on grid to coordinates
    /// </summary>
    /// <para>
    ///     Also handles updating of GridCubes
    /// </para>
    /// <param name="coordinates">Target coordinates</param>
    /// <returns>Target position</returns>
    public Vector3 MovePartOnGrid(Coord coordinates)
    {
        // TODO: ► Clear part's GridCubes at current position
        // foreach (var cube in occupiedGridCubes)
        // {
        //     cube.SetPart();
        // }
        
        // Distribute the part over GridCubes
        // occupiedGridCubes.Clear();
        var cubes = Grid3D.GetGridCubesInArea(coordinates, gridWorldDimensions.x, gridWorldDimensions.z);
        foreach (var cube in cubes)
        {
            cube.SetPart(gameObject);
            occupiedGridCubes.Add(cube);
        }

        return PositionPart(cubes);
    }
    
    public static GameObject GetPartAtCoords(Coord coordinates)
    {
        return Grid3D.GetGridCubeAt(coordinates).GetPart();
    }

    public void ApplyTransform()
    {
        // Check, if position & rotation can be applied
        // Then apply
        // Or just return bool?
    }
}
