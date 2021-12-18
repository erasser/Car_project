/// <summary>
///     Used as grid indexes, hold integer values.
/// <para>
///     Floats are rounded down.
/// </para>
/// </summary>

// TODO: x & z coords are swapped or what (see MoveLeft() v MoveCloser)
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

    public Coord MoveUp()
    {
        ++y;
        if (y == yCount)
            y = 0;
        return this;
    }

    public Coord MoveDown()
    {
        --y;
        if (y == -1)
            y = yCount - 1;
        return this;
    }

    public Coord MoveLeft()
    {
        --z;
        if (z == -1)
            z = xCount - 1;
        return this;
    }

    public Coord MoveRight()
    {
        ++z;
        if (z == xCount)
            z = 0;
        return this;
    }

    public Coord MoveCloser()
    {
        --x;
        if (x == -1)
            x = zCount - 1;
        return this;
    }

    public Coord MoveFarther()
    {
        ++x;
        if (x == zCount)
            x = 0;
        return this;
    }
}