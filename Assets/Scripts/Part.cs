using System.Collections.Generic;
using UnityEngine;

// Všechny GridCube, přes které part zabírá místo, budou referencovat ten part

public class Part : MonoBehaviour
{
    public GridCube gridCube;         // The main GridCube, at which the part is held
    public List<GridCube> gridcubes;  // List of all GridCubes the part is taking up space, including the main gridCube ↑
    private byte _rotation;           // 0, 1, 2, 3

    private void Awake()
    {
        // UpdaGridCubes
    }

    // TODO: For bigger object it should be more sophisticated with change of coordinates (so 4-cubed remains at the same cubes)
    public void Rotate()
    {
        transform.eulerAngles = new Vector3(0, ++_rotation * 90, 0);  // Rotates right
    }
}
