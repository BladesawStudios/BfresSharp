namespace BfresSharp.Gx2.Addressing;

/// <summary>Chooses the tile mode a mip level is stored in.</summary>
/// <remarks>
/// A level keeps its surface's tile mode until it is too small to fill one macro tile, and then it falls back to a micro
/// tiled mode; a level with fewer than four slices cannot be thick. Bank swapped modes are treated as their plain
/// counterparts for the comparison.
/// </remarks>
internal static class Gx2MipTileMode
{
    public static uint Choose(uint baseMode, uint bpp, uint level, uint width, uint height, uint slices, uint samples)
    {
        uint mode = ForSamples(baseMode, samples);
        if (level == 0) return mode;

        if (bpp is 24 or 48 or 96) bpp /= 3;
        uint paddedWidth = Gx2Alignments.NextPow2(width);
        uint paddedHeight = Gx2Alignments.NextPow2(height);
        mode = Gx2TileModes.ToNonBankSwapped(mode);

        uint microTileBytes = (samples * bpp * (Gx2TileModes.Thickness(mode) << 6) + 7) >> 3;
        uint widthFactor = microTileBytes < 256 ? Math.Max(1, 256 / microTileBytes) : 1;
        mode = FitMacroTile(mode, paddedWidth, paddedHeight, widthFactor);

        if (Gx2Alignments.NextPow2(slices) < 4) mode = mode switch
        {
            Gx2TileModes.Micro1dThick => Gx2TileModes.Micro1dThin,
            Gx2TileModes.Macro2dThick => Gx2TileModes.Macro2dThin1,
            Gx2TileModes.Macro3dThick => Gx2TileModes.Macro3dThin1,
            _ => mode,
        };

        return ForSamples(mode, samples);
    }

    // Multisampled micro tiled surfaces move to a macro tiled mode, and thick ones to a thin one.
    private static uint ForSamples(uint mode, uint samples) => mode switch
    {
        Gx2TileModes.Micro1dThin when samples > 1 => Gx2TileModes.Macro2dThin1,
        Gx2TileModes.Micro1dThick => samples is 2 or 4 ? Gx2TileModes.Macro2dThick : samples > 1 ? Gx2TileModes.Micro1dThin : mode,
        _ => mode,
    };

    private static uint FitMacroTile(uint mode, uint width, uint height, uint widthFactor)
    {
        (uint tileWidth, uint tileHeight) = mode switch
        {
            Gx2TileModes.Macro2dThin2 => (16u, 32u),
            Gx2TileModes.Macro2dThin4 => (8u, 64u),
            _ => (32u, 16u),
        };

        bool small = width < widthFactor * tileWidth || height < tileHeight;
        if (!small) return mode;
        return mode switch
        {
            Gx2TileModes.Macro2dThin1 or Gx2TileModes.Macro3dThin1 or Gx2TileModes.Macro2dThin2 or Gx2TileModes.Macro2dThin4 => Gx2TileModes.Micro1dThin,
            Gx2TileModes.Macro2dThick or Gx2TileModes.Macro3dThick => Gx2TileModes.Micro1dThick,
            _ => mode,
        };
    }
}
