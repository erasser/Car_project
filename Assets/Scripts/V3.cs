/// <summary>
///     Used as grid indexes, hold integer values.
///     Floats are rounded down.
/// </summary>
internal struct V3
{
    public int x;
    public int y;
    public int z;

    public V3(int x, int y, int z)  // This automatically rounds a float down.
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}