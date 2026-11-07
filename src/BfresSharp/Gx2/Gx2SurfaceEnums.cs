#pragma warning disable 1591

namespace BfresSharp.Gx2;

/// <summary>
/// Represents shapes of a given surface or texture.
/// </summary>
public enum GX2SurfaceDim : uint
{
    Dim1D,
    Dim2D,
    Dim3D,
    DimCube,
    Dim1DArray,
    Dim2DArray,
    Dim2DMsaa,
    Dim2DMsaaArray
}

/// <summary>
/// Represents desired texture, color-buffer, depth-buffer, or scan-buffer formats.
/// </summary>
public enum GX2SurfaceFormat : uint
{
    Invalid = 0x00000000,
    TC_R8_UNorm = 0x00000001,
    TC_R8_UInt = 0x00000101,
    TC_R8_SNorm = 0x00000201,
    TC_R8_SInt = 0x00000301,
    T_R4_G4_UNorm = 0x00000002,
    TCD_R16_UNorm = 0x00000005,
    TC_R16_UInt = 0x00000105,
    TC_R16_SNorm = 0x00000205,
    TC_R16_SInt = 0x00000305,
    TC_R16_Float = 0x00000806,
    TC_R8_G8_UNorm = 0x00000007,
    TC_R8_G8_UInt = 0x00000107,
    TC_R8_G8_SNorm = 0x00000207,
    TC_R8_G8_SInt = 0x00000307,
    TCS_R5_G6_B5_UNorm = 0x00000008,
    TC_R5_G5_B5_A1_UNorm = 0x0000000A,
    TC_R4_G4_B4_A4_UNorm = 0x0000000B,
    TC_A1_B5_G5_R5_UNorm = 0x0000000C,
    TC_R32_UInt = 0x0000010D,
    TC_R32_SInt = 0x0000030D,
    TCD_R32_Float = 0x0000080E,
    TC_R16_G16_UNorm = 0x0000000F,
    TC_R16_G16_UInt = 0x0000010F,
    TC_R16_G16_SNorm = 0x0000020F,
    TC_R16_G16_SInt = 0x0000030F,
    TC_R16_G16_Float = 0x00000810,
    D_D24_S8_UNorm = 0x00000011,
    T_R24_UNorm_X8 = 0x00000011,
    T_X24_G8_UInt = 0x00000111,
    D_D24_S8_Float = 0x00000811,
    TC_R11_G11_B10_Float = 0x00000816,
    TCS_R10_G10_B10_A2_UNorm = 0x00000019,
    TC_R10_G10_B10_A2_UInt = 0x00000119,
    TC_R10_G10_B10_A2_SNorm = 0x00000219,
    TC_R10_G10_B10_A2_SInt = 0x00000319,
    TCS_R8_G8_B8_A8_UNorm = 0x0000001A,
    TC_R8_G8_B8_A8_UInt = 0x0000011A,
    TC_R8_G8_B8_A8_SNorm = 0x0000021A,
    TC_R8_G8_B8_A8_SInt = 0x0000031A,
    TCS_R8_G8_B8_A8_SRGB = 0x0000041A,
    TCS_A2_B10_G10_R10_UNorm = 0x0000001B,
    TC_A2_B10_G10_R10_UInt = 0x0000011B,
    D_D32_Float_S8_UInt_X24 = 0x0000081C,
    T_R32_Float_X8_X24 = 0x0000081C,
    T_X32_G8_UInt_X24 = 0x0000011C,
    TC_R32_G32_UInt = 0x0000011D,
    TC_R32_G32_SInt = 0x0000031D,
    TC_R32_G32_Float = 0x0000081E,
    TC_R16_G16_B16_A16_UNorm = 0x0000001F,
    TC_R16_G16_B16_A16_UInt = 0x0000011F,
    TC_R16_G16_B16_A16_SNorm = 0x0000021F,
    TC_R16_G16_B16_A16_SInt = 0x0000031F,
    TC_R16_G16_B16_A16_Float = 0x00000820,
    TC_R32_G32_B32_A32_UInt = 0x00000122,
    TC_R32_G32_B32_A32_SInt = 0x00000322,
    TC_R32_G32_B32_A32_Float = 0x00000823,
    T_BC1_UNorm = 0x00000031,
    T_BC1_SRGB = 0x00000431,
    T_BC2_UNorm = 0x00000032,
    T_BC2_SRGB = 0x00000432,
    T_BC3_UNorm = 0x00000033,
    T_BC3_SRGB = 0x00000433,
    T_BC4_UNorm = 0x00000034,
    T_BC4_SNorm = 0x00000234,
    T_BC5_UNorm = 0x00000035,
    T_BC5_SNorm = 0x00000235,
    T_NV12_UNorm = 0x00000081
}

/// <summary>
/// Represents Indicates how a given surface may be used. A final TV render target is one that will be copied to a
/// TV scan buffer. It needs to be designated to handle certain display corner cases (when a HD surface must be
/// scaled down to display in NTSC/PAL).
/// </summary>
[Flags]
public enum GX2SurfaceUse : uint
{
    Texture = 1 << 0,
    ColorBuffer = 1 << 1,
    DepthBuffer = 1 << 2,
    ScanBuffer = 1 << 3,
    FinalTV = 1u << 31,
    ColorBufferTexture = Texture | ColorBuffer,
    DepthBufferTexture = Texture | DepthBuffer,
    ColorBufferFinalTV = FinalTV | ColorBuffer,
    ColorBufferTextureFinalTV = FinalTV | ColorBufferTexture
}

/// <summary>
/// Represents the desired tiling modes for a surface.
/// </summary>
public enum GX2TileMode : uint
{
    Default,
    LinearAligned,
    Mode1dTiledThin1,
    Mode1dTiledThick,
    Mode2dTiledThin1,
    Mode2dTiledThin2,
    Mode2dTiledThin4,
    Mode2dTiledThick,
    Mode2bTiledThin1,
    Mode2bTiledThin2,
    Mode2bTiledThin4,
    Mode2bTiledThick,
    Mode3dTiledThin1,
    Mode3dTiledThick,
    Mode3bTiledThin1,
    Mode3bTiledThick,
    LinearSpecial
}

#pragma warning restore 1591
