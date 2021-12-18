using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/// <summary>
///     Servers as objects holder
/// </summary>
public class GridCube
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
    ///     Held part object
    /// </summary>
    public GameObject part;

    /// <summary>
    ///     List of coordinates of adjacent cubes, which the held object also takes up
    /// </summary>
    public List<Coord> adjacentCoords;

    public GridCube(Vector3 coordinates = new(), GameObject trackPart = null)
    {
        position = coordinates;
        part = trackPart;
        adjacentCoords = new List<Coord>();
    }

    public void SetPart(GameObject partToSet)
    {
        part = partToSet;
    }
}
