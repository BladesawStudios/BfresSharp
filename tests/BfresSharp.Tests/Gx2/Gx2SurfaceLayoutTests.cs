using BfresSharp.Errors;
using BfresSharp.Gx2;
using BfresSharp.Gx2.Addressing;

namespace BfresSharp.Tests.Gx2;

/// <summary>Checks the level sizes against values the console's address library gives.</summary>
public sealed class Gx2SurfaceLayoutTests
{
    // Pitch, height, size, base alignment, stored tile mode, element bits.
    [Theory]
    [InlineData(GX2SurfaceFormat.T_BC1_UNorm, 256, 256, GX2TileMode.Mode2dTiledThin1, 0, 64u, 64u, 32768u, 4096u, 4u, 64u)]
    [InlineData(GX2SurfaceFormat.T_BC1_UNorm, 256, 256, GX2TileMode.Mode2dTiledThin1, 2, 16u, 16u, 2048u, 256u, 2u, 64u)]
    [InlineData(GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, 100, 60, GX2TileMode.Mode2dTiledThin1, 0, 128u, 64u, 32768u, 2048u, 4u, 32u)]
    [InlineData(GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, 100, 60, GX2TileMode.Mode2dTiledThin1, 3, 16u, 8u, 512u, 256u, 2u, 32u)]
    [InlineData(GX2SurfaceFormat.T_BC3_UNorm, 512, 512, GX2TileMode.Mode2dTiledThin1, 2, 32u, 32u, 16384u, 8192u, 4u, 128u)]
    [InlineData(GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, 64, 64, GX2TileMode.LinearAligned, 2, 64u, 16u, 4096u, 256u, 1u, 32u)]
    [InlineData(GX2SurfaceFormat.TC_R8_UNorm, 33, 17, GX2TileMode.Mode1dTiledThin1, 0, 64u, 24u, 1536u, 256u, 2u, 8u)]
    public void Levels_match_the_address_library(GX2SurfaceFormat format, uint width, uint height, GX2TileMode mode, int level, uint pitch, uint rows, uint size, uint align, uint stored, uint bits)
    {
        Gx2LevelInfo info = Gx2SurfaceLayout.GetLevel(format, width, height, 1, GX2SurfaceDim.Dim2D, mode, 0, level);
        Assert.Equal((pitch, rows, size, align, stored, bits), (info.Pitch, info.Height, info.Size, info.BaseAlign, info.TileMode, info.BitsPerElement));
    }

    [Fact]
    public void A_cube_has_at_least_six_faces()
    {
        Gx2LevelInfo info = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNorm, 64, 64, 1, GX2SurfaceDim.DimCube, GX2TileMode.Mode2dTiledThin1, 0, 0);
        Assert.True(info.Depth >= 6);
    }

    [Fact]
    public void The_special_linear_mode_has_no_padding()
    {
        Gx2LevelInfo info = Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.T_BC1_UNorm, 30, 30, 1, GX2SurfaceDim.Dim2D, GX2TileMode.LinearSpecial, 0, 0);
        Assert.Equal((8u, 8u, 512u), (info.Pitch, info.Height, info.Size));
    }

    [Fact]
    public void An_invalid_format_is_a_format_error()
    {
        Assert.Throws<BfresFormatException>(() => Gx2SurfaceLayout.GetLevel(GX2SurfaceFormat.Invalid, 8, 8, 1, GX2SurfaceDim.Dim2D, GX2TileMode.Mode2dTiledThin1, 0, 0));
    }
}
