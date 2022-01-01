using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Servers as objects holder.
/// </summary>
public class GridCube  // Can't be a struct, because reference type is needed
{
    /// <summary>
    ///     Position of the cube in world space.
    /// </summary>
    public Vector3 position;

    /// <summary>
    ///     Coordinates of the cube.
    /// </summary>
    public Coord coordinates;
    
    /// <summary>
    ///     List of part objects.
    /// </summary>
    readonly List<Part> _parts;     // Part would be more efficient, since no GetComponent() is needed

    public GridCube(Vector3 positionParam, Coord coordinatesParam)
    {
        position = positionParam;
        coordinates = coordinatesParam;
        _parts = new();
    }

    public int GetPartsCount()
    {
        return _parts.Count;
    }

    public bool IsOccupied()
    {
        return GetPartsCount() > 0;
    }
    
    public Part GetPart()
    {
        return _parts.Count == 1 ? _parts[0] : null;  // Only one part can occupy a cube (when not transforming)
    }
    
    public void SetPart(Part partToSet = null)
    {
        _parts.Add(partToSet);
    }

    public void UnsetPart(Part partToUnset)
    {
        _parts.Remove(partToUnset);
    }

    public void Clear()
    {
        _parts.Clear();
    }

    /// <summary>
    ///     Checks if the cube is valid.
    /// </summary>
    /// <para>
    ///     A cube can be occupied by 0 or 1 parts, cubes on grid edges can't be occupied at all.
    /// </para>
    public bool IsValid()
    {
        // Can contain max 1 part
        if (GetPartsCount() > 1)
            return false;
    
        // Can't contain a part at the edge
        if (IsOccupied() &&
            (coordinates.x == 0 || coordinates.y == 0 || coordinates.z == 0 ||
             coordinates.x == Grid3D.instance.xCount - 1 || coordinates.y == Grid3D.instance.yCount - 1 || coordinates.z == Grid3D.instance.zCount - 1))
            return false;
    
        return true;
    }

    /// <summary>
    ///     Checks whether all provided GridCubes are valid (i.e. none contain more than 1 part or is at the edge of grid)
    /// </summary>
    /// <param name="cubes">List of cubes to test.</param>
    public static bool AreCubesValid(List<GridCube> cubes)
    {
        var areValid = true;
        
        foreach (var cube in cubes)
        {
            if (!cube.IsValid())
            {
                areValid = false;
                break;
            }
        }
        return areValid;
    }

    /// <summary>
    ///     The cube is occupied and valid.
    /// </summary>
    // public bool ShouldBeSaved()
    // {
    //     return IsOccupied() && IsValid();
    // }

    // public GridCubeSaveData GetGridCubeSaveData()
    // {
    //     return new GridCubeSaveData(coordinates);
    // }
}

// [Serializable]
// public struct GridCubeSaveData
// {
//     public Coord coordinates;
//     
//     public GridCubeSaveData(Coord coordinates)
//     {
//         this.coordinates = coordinates;
//     }
// }