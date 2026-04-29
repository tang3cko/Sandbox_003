namespace SwarmSurvivor;

public static class ProjectilePaletteCalculator
{
    public const int PaletteSize = 8;

    /// <summary>
    /// Wraps a colorIdx (possibly negative or >= paletteSize) to a valid 0..paletteSize-1 index.
    /// </summary>
    public static int WrapPaletteIndex(int colorIdx, int paletteSize)
    {
        if (paletteSize <= 0) return 0;
        return ((colorIdx % paletteSize) + paletteSize) % paletteSize;
    }

    /// <summary>
    /// Convenience overload using PaletteSize default.
    /// </summary>
    public static int WrapPaletteIndex(int colorIdx)
    {
        return WrapPaletteIndex(colorIdx, PaletteSize);
    }
}
