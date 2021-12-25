using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/// <summary>
///     Servers as objects holder
/// </summary>
public class GridCube  // Can't be a struct, because reference type is needed
{
    /// <summary>
    ///     Position of the cube in world space
    /// </summary>
    public Vector3 position;

    /// <summary>
    ///     Coordinates of the cube - just for debug at the moment
    /// </summary>
    public Coord coordinates;
    
    /// <summary>
    ///     List of part objects
    /// </summary>
    public List<GameObject> parts;

    /// <summary>
    ///     List of coordinates of adjacent cubes, which the held object also occupies
    /// </summary>
    public List<Coord> adjacentCoords;  // If i use this, change the name accordingly to Parts.cs gridCubes var

    public GridCube(Vector3 positionParam, Coord coordinatesParam)
    {
        position = positionParam;
        coordinates = coordinatesParam;
        parts = new();
        adjacentCoords = new List<Coord>();
    }

    public int GetPartsCount()
    {
        return parts.Count;
    }
    
    public GameObject GetPart()
    {
        return parts.Count == 1 ? parts[0] : null;  // Only one part can occupy a cube (when not transforming)
    }
    
    public void SetPart(GameObject partToSet = null)
    {
        parts.Add(partToSet);
    }

    public void UnsetPart(GameObject partToUnset)
    {
        parts.Remove(partToUnset);
    }
}
