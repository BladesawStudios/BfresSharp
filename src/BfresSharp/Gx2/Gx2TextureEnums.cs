#pragma warning disable 1591

namespace BfresSharp.Gx2;

/// <summary>
/// Represents the source channels to map to a color channel in textures.
/// </summary>
public enum GX2CompSel : byte
{
    ChannelR,
    ChannelG,
    ChannelB,
    ChannelA,
    Always0,
    Always1
}

/// <summary>
/// Represents maximum desired anisotropic filter ratios. Higher ratios give better image quality, but slower
/// performance.
/// </summary>
public enum GX2TexAnisoRatio : uint
{
    Ratio_1_1,
    Ratio_2_1,
    Ratio_4_1,
    Ratio_8_1,
    Ratio_16_1,
}

/// <summary>
/// Represents type of border color to use.
/// </summary>
public enum GX2TexBorderType : uint
{
    ClearBlack,
    SolidBlack,
    SolidWhite,
    UseRegister
}

/// <summary>
/// Represents how to treat texture coordinates outside of the normalized coordinate texture range.
/// </summary>
public enum GX2TexClamp : uint
{
    Wrap,
    Mirror,
    Clamp,
    MirrorOnce,
    ClampHalfBorder,
    MirrorOnceHalfBorder,
    ClampBorder,
    MirrorOnceBorder,
    ClampToEdge,
}

/// <summary>
/// Represents desired texture filter options between mip levels.
/// </summary>
public enum GX2TexMipFilterType : uint
{
    NoMip,
    Point,
    Linear
}

/// <summary>
/// Represents desired texture filter options within a plane.
/// </summary>
public enum GX2TexXYFilterType : uint
{
    Point,
    Bilinear
}

/// <summary>
/// Represents desired texture filter options between Z planes.
/// </summary>
public enum GX2TexZFilterType : uint
{
    UseXY,
    Point,
    Linear
}
