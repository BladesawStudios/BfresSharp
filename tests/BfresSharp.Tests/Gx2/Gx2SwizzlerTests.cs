using BfresSharp.Errors;
using BfresSharp.Gx2;
using BfresSharp.Gx2.Addressing;

namespace BfresSharp.Tests.Gx2;

/// <summary>Checks that tiling moves every pixel somewhere of its own and back, and the placement of known pixels.</summary>
public sealed class Gx2SwizzlerTests
{
    public static TheoryData<GX2SurfaceFormat, GX2TileMode, uint, uint, uint> Surfaces()
    {
        var data = new TheoryData<GX2SurfaceFormat, GX2TileMode, uint, uint, uint>();
        GX2SurfaceFormat[] formats = [GX2SurfaceFormat.TC_R8_UNorm, GX2SurfaceFormat.TC_R8_G8_UNorm, GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, GX2SurfaceFormat.TC_R16_G16_B16_A16_Float, GX2SurfaceFormat.T_BC1_UNorm, GX2SurfaceFormat.T_BC3_UNorm];
        foreach (GX2SurfaceFormat format in formats)
        {
            foreach (GX2TileMode mode in new[] { GX2TileMode.LinearAligned, GX2TileMode.Mode1dTiledThin1, GX2TileMode.Mode2dTiledThin1, GX2TileMode.Mode2dTiledThin2, GX2TileMode.Mode2dTiledThin4, GX2TileMode.Mode2bTiledThin1, GX2TileMode.Mode3dTiledThin1 })
            {
                foreach ((uint width, uint height, uint swizzle) in new[] { (64u, 64u, 0u), (100u, 37u, 0x300u), (5u, 3u, 0x500u) }) data.Add(format, mode, width, height, swizzle);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Surfaces))]
    public void Every_pixel_goes_to_a_place_of_its_own_and_back(GX2SurfaceFormat format, GX2TileMode mode, uint width, uint height, uint swizzle)
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(format, width, height, 1, GX2SurfaceDim.Dim2D, mode, 0, 0);
        var rows = new byte[Gx2Swizzler.LinearSize(level)];
        new Random(5).NextBytes(rows);
        var surface = new byte[level.Size];
        Gx2Swizzler.Swizzle(level, 0, swizzle, rows, surface);
        var back = new byte[rows.Length];
        Gx2Swizzler.Deswizzle(level, 0, swizzle, surface, back);
        Assert.Equal(rows, back);
    }

    [Fact]
    public void A_micro_tile_interleaves_the_bits_of_x_and_y()
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TC_R8_UNorm, 8, 8, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode1dTiledThin1, 0, 0);
        var rows = new byte[64];
        rows[1] = 1; // x = 1, y = 0
        rows[8] = 2; // x = 0, y = 1
        rows[16] = 3; // x = 0, y = 2
        var surface = new byte[level.Size];
        Gx2Swizzler.Swizzle(level, 0, 0, rows, surface);
        Assert.Equal(1, surface[1]);
        Assert.Equal(2, surface[16]);
        Assert.Equal(3, surface[8]);
    }

    [Fact]
    public void Elements_past_the_end_of_the_surface_are_skipped()
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, 64, 64, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode2dTiledThin1, 0, 0);
        var rows = new byte[Gx2Swizzler.LinearSize(level)];
        Gx2Swizzler.Deswizzle(level, 0, 0, new byte[100], rows);
        Assert.All(rows, value => Assert.Equal(0, value));
    }

    [Fact]
    public void A_short_row_buffer_is_refused()
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TC_R8_UNorm, 8, 8, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode1dTiledThin1, 0, 0);
        Assert.Throws<ArgumentException>(() => Gx2Swizzler.Deswizzle(level, 0, 0, new byte[level.Size], new byte[10]));
    }

    [Fact]
    public void A_slice_past_the_depth_is_refused()
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TC_R8_UNorm, 8, 8, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode1dTiledThin1, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => Gx2Swizzler.Deswizzle(level, 3, 0, new byte[level.Size], new byte[64]));
    }

    [Fact]
    public void Multisampled_surfaces_are_reported_as_unsupported()
    {
        Gx2LevelInfo level = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TC_R8_UNorm, 8, 8, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode2dTiledThin1, 1, 0);
        Assert.Throws<BfresUnsupportedException>(() => Gx2Swizzler.Deswizzle(level, 0, 0, new byte[level.Size], new byte[64]));
    }
}
