using System.Collections.Generic;
using UnityEngine;

// Všechny GridCube, přes které part zabírá místo, budou referencovat ten part

public class Part : MonoBehaviour
{
    public GridCube gridCube;         // The main GridCube, at which the part is held
    private Coord _gridDimensions;    // Count of GridCubes, that the part is going to occupy
    public List<GridCube> gridCubes;  // List of all GridCubes the part is occupying, including the main gridCube ↑ // TODO: I'm not going to use main cube
    private byte _rotation;           // 0, 1, 2, 3

    private void Awake()
    {
        CalculateGridCubes();
    }

    public void Rotate()
    {
        transform.eulerAngles = new Vector3(0, ++_rotation * 90, 0);  // Rotates right
    }

    /// <summary>
    ///     Calculates GridCubes count for each axis based on mesh dimensions
    /// </summary>
    private void CalculateGridCubes()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;

        var gridCountX = GetCubesCount(mesh.bounds.size.x);
        var gridCountY = GetCubesCount(mesh.bounds.size.y);
        var gridCountZ = GetCubesCount(mesh.bounds.size.z);

        int GetCubesCount(float meshDimension)
        {
            // Subtract .1 to fix model vertices position inaccuracy
            // 10 units is the edge size of one cube
            // Must be at least 1
            return Mathf.Max(Mathf.CeilToInt((meshDimension - .1f) / 10), 1);
        }
    }
}
