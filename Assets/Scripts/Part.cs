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
        // TODO: occupiedGridCubes.Clear();  // clear cubes.cs also  // See MovePartOnGrid()
        // TODO: update UpdateCanTransformBeApplied() when it's done

        print("rotate!");
        transform.eulerAngles = new (0, ++_rotation * 90, 0);  // Rotates right
        UpdateWorldCubeDimensions();

    }

    void UpdateWorldCubeDimensions()
    {
        /*  IMPLEMENT THE FOLLOWING HERE  */

        // TODO: V Rotate() se volá UpdateWorldCubeDimensions()
        // TODO: Check, if rotation is possible
        // For ■■ shape
        if (gridWorldDimensions.x == 1 && gridWorldDimensions.z == 2) // from _ to |
        {
            Grid3D.GetGridCubeAt(new Coord(occupiedGridCubes[0].coordinates.x, occupiedGridCubes[0].coordinates.y, occupiedGridCubes[0].coordinates.z + 1))
                .SetPart(this);
            occupiedGridCubes[1].UnsetPart(this);
        }
        else if (gridWorldDimensions.x == 2 && gridWorldDimensions.z == 1) // from | to _
        {
            // TODO: ► Zde jsem skončil. Vyřešit.
            Grid3D.GetGridCubeAt(new Coord(occupiedGridCubes[0].coordinates.x, occupiedGridCubes[0].coordinates.y, occupiedGridCubes[0].coordinates.z + 1))
                .SetPart(this);
            occupiedGridCubes[1].UnsetPart(this);
        }

        
        if (_rotation is 0 or 2) // :-o
            gridWorldDimensions = _gridLocalDimensions;
        else
        {
            gridWorldDimensions.x = _gridLocalDimensions.z;
            gridWorldDimensions.z = _gridLocalDimensions.x;
        }
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
        return transform.position = new(
            (gridCubes[^1].position.x + gridCubes[0].position.x) / 2,
            gridCubes[0].position.y,
            (gridCubes[^1].position.z + gridCubes[0].position.z) / 2);
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
