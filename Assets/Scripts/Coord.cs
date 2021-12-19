using System;

/// <summary>
///     Used as grid indexes, hold integer values.
/// <para>
///     Used also as a relative count of cubes, which each part occupy
/// </para>
/// <para>
///     Floats are rounded down.
/// </para>
/// </summary>

public struct Coord
{
    public int x;
    public int y;
    public int z;

    public static Coord zero = new(0, 0, 0);

    public static int xCount = 7;
    public static int yCount = 5;
    public static int zCount = 5;

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
        if (x < xCount - 1)
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
        if (y < yCount - 1)
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
        if (z < zCount - 1)
            ++z;
        return this;
    }
}