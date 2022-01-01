using System;

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
        if (axis == "x")
            return x;
        if (axis == "y")
            return y;
        if (axis == "z")
            return z;

        throw new Exception($"You tried to set axis \"{axis}\", which is bullshit.");
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

    public Coord MoveLeft()
    {
        if (x > 0)
            --x;
        return this;
    }

    public Coord MoveRight()
    {
        if (x < Grid3D.instance.xCount - 1)
            ++x;
        return this;
    }

    public Coord MoveDown()
    {
        if (y > 0)
            --y;
        return this;
    }
    
    public Coord MoveUp()
    {
        if (y < Grid3D.instance.yCount - 1)
            ++y;
        return this;
    }

    public Coord MoveCloser()
    {
        if (z > 0)
            --z;
        return this;
    }

    public Coord MoveFarther()
    {
        if (z < Grid3D.instance.zCount - 1)
            ++z;
        return this;
    }
}