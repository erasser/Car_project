using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Servers as objects holder
/// </summary>
public class GridCube  // Can't be a struct, because reference type is needed
{
    /// <summary>
    ///     Coordinates of held object
    /// </summary>
    // public Coord coord;

    /// <summary>
    ///     Position of the cube in world space
    /// </summary>
    public Vector3 position;

    /// <summary>
    ///     Coordinates of the cube - just for debug at the moment
    /// </summary>
    public Coord coordinates;
    
    /// <summary>
    ///     Held part object
    /// </summary>
    public GameObject part;

    /// <summary>
    ///     List of coordinates of adjacent cubes, which the held object also occupies
    /// </summary>
    public List<Coord> adjacentCoords;  // If i use this, change the name accordingly to Parts.cs gridCubes var

    public GridCube(Vector3 positionParam, Coord coordinatesParam)
    {
        position = positionParam;
        coordinates = coordinatesParam;
        part = null;
        adjacentCoords = new List<Coord>();
    }
    
    public void SetPart(GameObject partToSet)
    {
        part = partToSet;
    }
}
