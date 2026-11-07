using BfresSharp.Errors;

namespace BfresSharp.Gx2.Addressing;

/// <summary>Works out the stored size and alignment of any mip level of a GX2 surface.</summary>
/// <remarks>
/// The numbers are those of the console's address library, so the offsets and sizes agree with the files its tools write.
/// Pitch, height and depth of the result are padded; a block compressed format counts in 4x4 blocks. The special linear
/// mode has no padding at all. The calculation reads only the surface description, so it is safe to call from any thread.
/// </remarks>
public static class Gx2SurfaceLayout
{
    public static Gx2LevelInfo GetLevel(GX2SurfaceFormat format, uint width, uint height, uint depth, GX2SurfaceDim dim, GX2TileMode tileMode, uint aa, int level)
    {
        if ((uint)dim > (uint)GX2SurfaceDim.Dim2DMsaaArray) throw new BfresFormatException($"The surface dimension {(uint)dim} is not one GX2 has.");
        uint hardware = Gx2FormatInfo.HardwareFormat(format);
        (uint elementBits, uint expandX, uint expandY, uint elementMode) = Gx2FormatInfo.Element(hardware);
        if (hardware == 0 || elementBits == 0) throw new BfresFormatException($"The surface format {(uint)format:X} is not one GX2 has.");

        uint samples = 1u << (int)aa;
        uint mip = (uint)level;
        (uint levelHeight, uint slices, bool cube) = Dimensions(dim, height, depth, mip);
        uint levelWidth = Math.Max(1, width >> level);
        if (tileMode == GX2TileMode.LinearSpecial) return Special(hardware, levelWidth, levelHeight, slices, samples);
        bool blocks = elementMode is >= 9 and <= 13;
        uint pictureWidth = blocks ? (levelWidth + 3) / 4 : levelWidth;
        uint pictureHeight = blocks ? (levelHeight + 3) / 4 : levelHeight;

        uint mode = (uint)tileMode & 0x0F;
        if (blocks) (levelWidth, levelHeight) = BlockCompressedSize(levelWidth, levelHeight, mip);

        bool threeByte = elementMode == 4 && expandX == 3 && mode == Gx2TileModes.LinearAligned;
        (levelWidth, levelHeight, uint bpp) = Expand(elementMode, expandX, expandY, elementBits, levelWidth, levelHeight);
        uint sizeMode = Gx2MipTileMode.Choose(mode, bpp, mip, levelWidth, levelHeight, slices, samples);
        uint padDims = cube && mip == 0 ? 2u : 0u;
        var request = new Gx2SizeRequest(sizeMode, mode, bpp, samples, levelWidth, levelHeight, slices, mip, padDims, cube, threeByte);
        return Gx2LevelSizer.Size(request) with { BitsPerElement = bpp, Samples = samples, ElementsWide = pictureWidth, ElementsHigh = pictureHeight };
    }

    // Block compressed levels are rounded to whole blocks at the first level and to powers of two below it.
    private static (uint Width, uint Height) BlockCompressedSize(uint width, uint height, uint mip)
    {
        if (mip == 0) return (Gx2Alignments.AlignPow2(width, 4), Gx2Alignments.AlignPow2(height, 4));
        return (Gx2Alignments.NextPow2(width), Gx2Alignments.NextPow2(height));
    }

    // Turns pixels into elements: blocks for compressed formats, three elements a pixel for the 24, 48 and 96 bit formats.
    private static (uint Width, uint Height, uint Bpp) Expand(uint mode, uint expandX, uint expandY, uint bits, uint width, uint height)
    {
        if (expandX > 1 || expandY > 1)
        {
            if (mode == 4)
            {
                width *= expandX;
                height *= expandY;
            }
            else if (mode is >= 9 and <= 13)
            {
                width /= expandX;
                height /= expandY;
            }
            else
            {
                width = (width + expandX - 1) / expandX;
                height = (height + expandY - 1) / expandY;
            }

            width = Math.Max(1, width);
            height = Math.Max(1, height);
        }

        uint bpp = mode switch
        {
            4 => bits / expandX / expandY,
            5 or 6 => expandY * expandX * bits,
            9 or 12 => 64,
            10 or 11 or 13 => 128,
            _ => bits,
        };
        return (width, height, bpp);
    }

    private static (uint Height, uint Slices, bool Cube) Dimensions(GX2SurfaceDim dim, uint height, uint depth, uint mip)
    {
        uint shifted = Math.Max(1, height >> (int)mip);
        return dim switch
        {
            GX2SurfaceDim.Dim1D => (1, 1, false),
            GX2SurfaceDim.Dim2D or GX2SurfaceDim.Dim2DMsaa => (shifted, 1, false),
            GX2SurfaceDim.Dim3D => (shifted, Math.Max(1, depth >> (int)mip), false),
            GX2SurfaceDim.DimCube => (shifted, Math.Max(6, depth), true),
            GX2SurfaceDim.Dim1DArray => (1, depth, false),
            _ => (shifted, depth, false),
        };
    }

    // The special linear mode keeps pixels as they are, in rows of the block width.
    private static Gx2LevelInfo Special(uint hardware, uint width, uint height, uint slices, uint samples)
    {
        uint block = Gx2FormatInfo.IsBlockCompressed(hardware) ? 4u : 1u;
        uint bits = Gx2FormatInfo.BitsPerPixel(hardware);
        uint pitch = Gx2Alignments.AlignPow2(width, block) / block;
        uint rows = Gx2Alignments.AlignPow2(height, block) / block;
        uint size = bits * samples * slices * rows * pitch >> 3;
        return new Gx2LevelInfo(pitch, rows, slices, size, 1, 1, 1, 1, Gx2TileModes.LinearGeneral, bits, samples, (width + block - 1) / block, (height + block - 1) / block);
    }
}
