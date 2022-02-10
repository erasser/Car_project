using System;
using static UnityEngine.Mathf;

/// <summary>
/// <para>
///     Used as grid indexes (absolute or relative), hold integer values.
/// </para>
/// <para>
///     Floats are rounded down.
/// </para>
/// </summary>

[Serializable]
public struct Coord
{
    public int x;
    public int y;
    public int z;

    public static Coord zero = new(0, 0, 0);
    public static Coord Null = new(2147483647, 2147483647, 2147483647);  // My dirty hack for null value

    public Coord(int xCoord, int yCoord, int zCoord)  // This automatically rounds a float down.
    {
        x = xCoord;
        y = yCoord;
        z = zCoord;
    }

    public override string ToString()
    {
        return $"Coord = x: {x}, y: {y}, z: {z}";
    }

    public bool Equals(Coord coord)
    {
        return x == coord.x && y == coord.y && z == coord.z;
    }
    public static Coord operator +(Coord coord1, Coord coord2)
    {
        return new Coord(coord1.x + coord2.x, coord1.y + coord2.y, coord1.z + coord2.z);
    }
    public static Coord operator -(Coord coord1, Coord coord2)
    {
        return new Coord(coord1.x - coord2.x, coord1.y - coord2.y, coord1.z - coord2.z);
    }

    public bool IsNull()
    {
        return Equals(Null);
    }

    public int Get(string axis)
    {
        return axis switch
        {
            "x" => x,
            "y" => y,
            "z" => z,
            _ => throw new Exception($"You tried to set axis \"{axis}\", which is bullshit.")
        };
    }

    public void Set(string axis, int value)
    {
        if (axis == "x")
            x = value;
        else if (axis == "y")
            y = value;
        else if (axis == "z")
            z = value;
        else
            throw new Exception($"You tried to set axis \"{axis}\", which is bullshit.");
    }

    public Coord MoveX(int increment = 1)
    {
        x = Clamp(x += increment, 1, Grid3D.instance.xCount - 2);
        return this;
    }

    public Coord MoveY(int increment = 1)
    {
        y = Clamp(y += increment, 1, Grid3D.instance.yCount - 2);
        return this;
    }

    public Coord MoveZ(int increment = 1)
    {
        z = Clamp(z += increment, 1, Grid3D.instance.zCount - 2);
        return this;
    }
}