using BfresSharp.Gx2;
using BfresSharp.Gx2.Addressing;

namespace BfresSharp.Tests.Gx2;

/// <summary>Checks the swizzler against a plain pixel by pixel walk of the same addresses, on whole tiles, edge tiles and short surfaces.</summary>
public sealed class Gx2SwizzlerReferenceTests
{
    public static TheoryData<GX2SurfaceFormat, GX2TileMode, uint, uint, uint> Surfaces()
    {
        var data = new TheoryData<GX2SurfaceFormat, GX2TileMode, uint, uint, uint>();
        GX2SurfaceFormat[] formats = [GX2SurfaceFormat.TC_R8_UNorm, GX2SurfaceFormat.TC_R8_G8_UNorm, GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, GX2SurfaceFormat.TC_R16_G16_B16_A16_Float, GX2SurfaceFormat.T_BC1_UNorm, GX2SurfaceFormat.T_BC3_UNorm, GX2SurfaceFormat.TC_R32_G32_B32_A32_Float];
        GX2TileMode[] modes = [GX2TileMode.LinearAligned, GX2TileMode.Mode1dTiledThin1, GX2TileMode.Mode2dTiledThin1, GX2TileMode.Mode2dTiledThin4, GX2TileMode.Mode2bTiledThin2, GX2TileMode.Mode3dTiledThin1];
        foreach (GX2SurfaceFormat format in formats)
        {
            foreach (GX2TileMode mode in modes)
            {
                foreach ((uint width, uint height, uint swizzle) in new[] { (1u, 1u, 0u), (64u, 64u, 0u), (100u, 37u, 0x300u), (513u, 129u, 0x500u), (8u, 200u, 0x100u) }) data.Add(format, mode, width, height, swizzle);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Surfaces))]
    public void Deswizzling_matches_a_pixel_by_pixel_walk(GX2SurfaceFormat format, GX2TileMode mode, uint width, uint height, uint swizzle)
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(format, width, height, 1, GX2SurfaceDim.Dim2D, mode, 0, 0);
        foreach (int cut in new[] { 0, 1, 1000 })
        {
            var surface = new byte[Math.Max(0, (int)level.Size - cut)];
            new Random(3).NextBytes(surface);
            var expected = new byte[Gx2Swizzler.LinearSize(level)];
            var actual = new byte[expected.Length];
            Walk(level, swizzle, surface, expected, false);
            Gx2Swizzler.Deswizzle(level, 0, swizzle, surface, actual);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(Surfaces))]
    public void Swizzling_matches_a_pixel_by_pixel_walk(GX2SurfaceFormat format, GX2TileMode mode, uint width, uint height, uint swizzle)
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(format, width, height, 1, GX2SurfaceDim.Dim2D, mode, 0, 0);
        foreach (int cut in new[] { 0, 1, 1000 })
        {
            var rows = new byte[Gx2Swizzler.LinearSize(level)];
            new Random(4).NextBytes(rows);
            int length = Math.Max(0, (int)level.Size - cut);
            var expected = new byte[length];
            var actual = new byte[length];
            Walk(level, swizzle, expected, rows, true);
            Gx2Swizzler.Swizzle(level, 0, swizzle, rows, actual);
            Assert.Equal(expected, actual);
        }
    }

    // The slow definition: every pixel is addressed on its own, and one that would run off the surface is left alone.
    private static void Walk(Gx2LevelInfo level, uint swizzle, byte[] surface, byte[] rows, bool toSurface)
    {
        uint bytes = level.BitsPerElement / 8;
        var tiles = new Gx2TileAddresser(level, swizzle, 0);
        for (uint y = 0; y < level.ElementsHigh; y++)
        {
            for (uint x = 0; x < level.ElementsWide; x++)
            {
                ulong address;
                if (Gx2TileModes.IsLinear(level.TileMode)) address = (y * (ulong)level.Pitch + x) * bytes;
                else
                {
                    (ulong start, uint pipeBank) = tiles.Origin(x / 8, y / 8);
                    address = tiles.Address(start, pipeBank, tiles.Offsets.Span[(int)(y % 8 * 8 + x % 8)]);
                }

                if (address + bytes > (ulong)surface.Length) continue;
                var inRows = new Span<byte>(rows, (int)((y * level.ElementsWide + x) * bytes), (int)bytes);
                var inSurface = new Span<byte>(surface, (int)address, (int)bytes);
                if (toSurface) inRows.CopyTo(inSurface);
                else inSurface.CopyTo(inRows);
            }
        }
    }
}
