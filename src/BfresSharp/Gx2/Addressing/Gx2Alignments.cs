namespace BfresSharp.Gx2.Addressing;

/// <summary>The alignments a surface of one tile mode must keep, and the padding that applies them.</summary>
/// <remarks>
/// The base alignment is the alignment of the surface's address; the pitch and height alignments are in elements. Macro
/// tiled modes also report the size of a macro tile. All the calculations are those of the address library the console
/// uses, so a size or offset computed here matches the files its tools write.
/// </remarks>
internal readonly record struct Gx2Alignments(uint Base, uint Pitch, uint Height, uint MacroWidth, uint MacroHeight)
{
    public static Gx2Alignments Linear(uint mode, uint bpp)
    {
        if (mode == Gx2TileModes.LinearGeneral) return new Gx2Alignments(1, bpp != 1 ? 1u : 8u, 1, 0, 0);
        return new Gx2Alignments(256, Math.Max(0x40, 2048 / bpp), 1, 0, 0);
    }

    public static Gx2Alignments MicroTiled(uint mode, uint bpp, uint samples)
    {
        if (bpp is 24 or 48 or 96) bpp /= 3;
        uint pitch = Math.Max(8, 256 / bpp / samples / Gx2TileModes.Thickness(mode));
        return new Gx2Alignments(256, pitch, 8, 0, 0);
    }

    public static Gx2Alignments MacroTiled(uint mode, uint bpp, uint samples)
    {
        uint thickness = Gx2TileModes.Thickness(mode);
        if (bpp is 24 or 48 or 96) bpp /= 3;
        else if (bpp == 3) bpp = 1;
        (uint width, uint height) = Gx2TileModes.MacroTileSize(mode);

        uint pitch = Math.Max(width, width * (256 / bpp / (8 * thickness) / samples));
        uint macroTileBytes = samples * ((bpp * height * width + 7) >> 3);
        uint baseAlign = thickness == 1
            ? Math.Max(macroTileBytes, (samples * height * bpp * pitch + 7) >> 3)
            : Math.Max(256, (4 * height * bpp * pitch + 7) >> 3);

        uint microTileBytes = (thickness * samples * (bpp << 6) + 7) >> 3;
        baseAlign /= microTileBytes < 2048 ? 1 : microTileBytes / 2048;
        return new Gx2Alignments(baseAlign, pitch, height, width, height);
    }

    // The pitch a bank swapped mode must be a multiple of, so the banks of neighbouring macro tiles alternate.
    public static uint BankSwappedWidth(uint mode, uint bpp, uint samples, uint pitch)
    {
        if (!Gx2TileModes.IsBankSwapped(mode)) return 0;
        uint bytesPerSample = 8 * bpp;
        uint slicesPerTile = bytesPerSample != 0 ? Math.Max(1, samples / (2048 / bytesPerSample)) : 1;
        if (Gx2TileModes.IsThickMacroTiled(mode)) samples = 4;

        uint bytesPerTileSlice = samples * bytesPerSample / slicesPerTile;
        uint swapWidth = Math.Max(1, 128 / bpp) * 32;
        uint heightBytes = samples * Gx2TileModes.AspectRatio(mode) * bpp * 2 / slicesPerTile;
        uint swapMax = 0x4000 / heightBytes;
        uint swapMin = 256 / bytesPerTileSlice;

        uint width = Math.Min(swapMax, Math.Max(swapMin, swapWidth));
        while (width >= 2 * pitch) width >>= 1;
        return width;
    }

    // Rounds the pitch, height and slice count up to the alignments. A pad of 0 pads every dimension.
    public static (uint Pitch, uint Height, uint Slices) Pad(uint mode, uint padDims, bool cube, uint pitchAlign, uint heightAlign, uint sliceAlign, uint pitch, uint height, uint slices)
    {
        uint thickness = Gx2TileModes.Thickness(mode);
        if (padDims == 0) padDims = 3;

        pitch = (pitchAlign & (pitchAlign - 1)) == 0 ? AlignPow2(pitch, pitchAlign) : (pitch + pitchAlign - 1) / pitchAlign * pitchAlign;
        if (padDims > 1) height = AlignPow2(height, heightAlign);
        if (padDims > 2 || thickness > 1)
        {
            if (cube) slices = NextPow2(slices);
            if (thickness > 1) slices = AlignPow2(slices, sliceAlign);
        }

        return (pitch, height, slices);
    }

    public static uint AlignPow2(uint value, uint alignment) => ~(alignment - 1) & (value + alignment - 1);

    public static uint NextPow2(uint value)
    {
        if (value >= 0x7FFFFFFF) return 0x80000000;
        uint next = 1;
        while (next < value) next *= 2;
        return next;
    }
}
