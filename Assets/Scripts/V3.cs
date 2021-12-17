/// <summary>
///     Used as grid indexes, hold integer values.
/// <para>
///     Floats are rounded down.
/// </para>
/// </summary>

// TODO: x & z coords are swapped or what (see MoveLeft() v MoveCloser)

internal struct V3
{
    public int x;
    public int y;
    public int z;

    public static V3 zero = new(0, 0, 0);
    
    public static int xCount = 7;
    public static int yCount = 5;
    public static int zCount = 5;

    public V3(int x, int y, int z)  // This automatically rounds a float down.
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public V3 MoveUp()
    {
        ++y;
        if (y == yCount)
            y = 0;
        return this;
    }

    public V3 MoveDown()
    {
        --y;
        if (y == -1)
            y = yCount - 1;
        return this;
    }

    public V3 MoveLeft()
    {
        --z;
        if (z == -1)
            z = xCount - 1;
        return this;
    }

    public V3 MoveRight()
    {
        ++z;
        if (z == xCount)
            z = 0;
        return this;
    }

    public V3 MoveCloser()
    {
        --x;
        if (x == -1)
            x = zCount - 1;
        return this;
    }

    public V3 MoveFarther()
    {
        ++x;
        if (x == zCount)
            x = 0;
        return this;
    }

}