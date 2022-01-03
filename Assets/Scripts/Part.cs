using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI;

// Všechny GridCube, přes které part zabírá místo, budou referencovat ten part

public class Part : MonoBehaviour
{
    // public GridCube gridCube;  // The main GridCube, at which the part is held  // occupiedGridCubes[0] is used now
    Coord _gridLocalDimensions; // Count of GridCubes, that the part is going to occupy in local space, calculated just once
    public Coord gridWorldDimensions;   // Count of GridCubes, that the part is going to occupy in world space (but with local position), updated on rotation
    public readonly List<GridCube> occupiedGridCubes = new();    // List of all GridCubes the part is occupying, including the main gridCube ↑
    byte _rotation;             // 0, 1, 2, 3
    [HideInInspector]
    public Outline outlineComponent;
    public int partIndex;   // index in partCategory prefab

    void Awake()
    {
        CalculateLocalCubeDimensions();
        outlineComponent = GetComponent<Outline>();
    }

    // void OnDestroy()
    // {
    //     OrbitCamera.CheckIfWatched(gameObject);  // I don't use attaching camera target to parts
    // }

    /// <summary>
    ///     Calculates GridCubes count for each axis based on mesh dimensions.
    /// </summary>
    void CalculateLocalCubeDimensions()
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

    public void Rotate()
    {
        if (++_rotation == 4)  // I tried using modulo, but it's not applicable to byte type
            _rotation = 0;

        transform.eulerAngles = new (0, _rotation * 90, 0);

        UpdateWorldCubeDimensions();

        DistributeOverGridCubes(occupiedGridCubes[0].coordinates);  // Also calls UpdateCanTransformBeApplied()
        
        PositionPart(occupiedGridCubes);
    }

    void UpdateWorldCubeDimensions()
    {
        if (_rotation is 0 or 2) // :-o
            gridWorldDimensions = _gridLocalDimensions;
        else
        {
            gridWorldDimensions.x = _gridLocalDimensions.z;
            gridWorldDimensions.z = _gridLocalDimensions.x;
        }

        // This should be solved by the code above ↑
        // For ■■ shape (It's presumed the part is oriented in the x axis)
        // if (gridWorldDimensions.x == 1 && gridWorldDimensions.z == 2) // from _ to |
        // {
        //     occupiedGridCubes[1].UnsetPart(this);  // the cube on the right
        //     Grid3D.GetGridCubeAt(new Coord(occupiedGridCubes[0].coordinates.x, occupiedGridCubes[0].coordinates.y, occupiedGridCubes[0].coordinates.z + 1))
        //         .SetPart(this);  // the cube to the back
        // }
        // else if (gridWorldDimensions.x == 2 && gridWorldDimensions.z == 1) // from | to _
        // {
        //     occupiedGridCubes[1].UnsetPart(this);  // the cube to the back
        //     Grid3D.GetGridCubeAt(new Coord(occupiedGridCubes[0].coordinates.x + 1, occupiedGridCubes[0].coordinates.y, occupiedGridCubes[0].coordinates.z))
        //         .SetPart(this);  // the cube to the right
        // }

    }

    /// <summary>
    ///     Clears occupied cubes and updates occupiedGridCubes based on new coordinates.
    /// </summary>
    /// <returns>List of occupied gridCubes</returns>
    List<GridCube> DistributeOverGridCubes(Coord coordinates)
    {
        ClearCubes();

        var cubes = Grid3D.GetGridCubesInArea(coordinates, gridWorldDimensions.x, gridWorldDimensions.z);  // cubes at new position
        foreach (var cube in cubes)
        {
            cube.SetPart(this);
            occupiedGridCubes.Add(cube);
        }

        TrackEditor.UpdateCanTransformBeApplied();

        return cubes;
    }

    Vector3 PositionPart(List<GridCube> gridCubes)
    {
        // GameObject.Find("CubeHelper1").transform.position = gridCubes[0].position;         
        // GameObject.Find("CubeHelper2").transform.position = gridCubes[^1].position;
        
        return transform.position = new(
            (gridCubes[0].position.x + gridCubes[^1].position.x) / 2,
            gridCubes[0].position.y,
            (gridCubes[0].position.z + gridCubes[^1].position.z) / 2);
    }

    /// <summary>
    ///     Moves track part on grid to coordinates.
    /// </summary>
    /// <para>
    ///     Also handles updating of GridCubes.
    /// </para>
    /// <param name="coordinates">Target coordinates</param>
    /// <returns>Target position</returns>
    public Vector3 MovePartOnGrid(Coord coordinates)  // Return value is not used, but part position is fucked ut without it, that's strange
    {
        var cubes = DistributeOverGridCubes(coordinates);
        return PositionPart(cubes);
    }

    public static Part GetPartAtCoords(Coord coordinates)
    {
        return Grid3D.GetGridCubeAt(coordinates).GetPart();
    }

    public void Delete()
    {
        if (TrackEditor.selectedPart == gameObject)
            TrackEditor.UnselectPart();

        ClearCubes();
        Destroy(gameObject);
        
        if (CompareTag("partStart"))
            TrackEditor.startPart = null;
    }

    void ClearCubes()
    {
        foreach (var cube in occupiedGridCubes)
        {
            cube.UnsetPart(this);
        }
        occupiedGridCubes.Clear();
    }

    public static List<PartSaveData> GetPartsSaveData()
    {
        List<PartSaveData> partsSaveData = new();

        foreach (Transform partTransform in TrackEditor.track.transform)
        {
            partsSaveData.Add(partTransform.GetComponent<Part>().GetPartSaveData());
        }
        return partsSaveData;
    }

    PartSaveData GetPartSaveData()
    {
        return new PartSaveData(partIndex, _rotation, occupiedGridCubes[0].coordinates);
    }
}

[Serializable]
public struct PartSaveData
{
    // public string tag;
    public int partIndex;
    public byte rotation;
    public Coord initialOccupiedGridCubeCoord;

    public PartSaveData(int partIndex, byte rotation, Coord initialOccupiedGridCubeCoord)
    {
        // this.tag = tag;
        this.partIndex = partIndex;
        this.rotation = rotation;
        this.initialOccupiedGridCubeCoord = initialOccupiedGridCubeCoord;
    }
}
