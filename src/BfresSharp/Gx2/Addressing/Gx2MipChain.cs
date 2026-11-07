namespace BfresSharp.Gx2.Addressing;

/// <summary>Where the mip levels of a GX2 surface sit in its data, in the form a texture file stores them.</summary>
/// <remarks>
/// Level 0 is the image. Every other level starts at the base alignment of its own level, after the one before it. The
/// stored offsets are 13 values: the first is the size of the image, which is where the mip data starts, and the others
/// are the offsets of levels 2 and up from the start of the mip data (level 1 is at offset 0). MipSize is the size of
/// all the levels after the first, including the padding between them. A micro tiled level that follows a macro tiled
/// one has no pipe and bank of its own, so it is placed where bits 8 to 10 of its address are those of the swizzle.
/// </remarks>
public readonly record struct Gx2MipChain(uint ImageSize, uint MipSize, uint[] Offsets)
{
    public const int StoredOffsets = 13;

    public static Gx2MipChain Compute(GX2SurfaceFormat format, uint width, uint height, uint depth, GX2SurfaceDim dim, GX2TileMode tileMode, uint aa, uint swizzle, int mipCount)
    {
        Gx2LevelInfo first = Gx2SurfaceLayout.GetLevel(format, width, height, depth, dim, tileMode, aa, 0);
        var offsets = new uint[StoredOffsets];
        if (mipCount < 2) return new Gx2MipChain(first.Size, 0, offsets);

        offsets[0] = first.Size;
        uint cursor = 0;
        bool afterMacro = Gx2TileModes.IsMacroTiled(first.TileMode);
        for (int level = 1; level < mipCount; level++)
        {
            Gx2LevelInfo info = Gx2SurfaceLayout.GetLevel(format, width, height, depth, dim, tileMode, aa, level);
            uint start = Gx2Alignments.AlignPow2(cursor, info.BaseAlign);
            if (level >= 2 && afterMacro && Gx2TileModes.IsMicroTiled(info.TileMode)) start = WithPipeBank(start, swizzle);
            if (level >= 2 && level <= StoredOffsets) offsets[level - 1] = start;
            cursor = start + info.Size;
            afterMacro = Gx2TileModes.IsMacroTiled(info.TileMode);
        }

        return new Gx2MipChain(first.Size, cursor, offsets);
    }

    // The first position at or after the start whose address bits 8 to 10 are those of the swizzle.
    private static uint WithPipeBank(uint start, uint swizzle)
    {
        uint placed = (start & ~0x7FFu) + (swizzle & 0x700);
        return placed < start ? placed + 0x800 : placed;
    }
}
