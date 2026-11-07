using BfresSharp.Gx2;

namespace BfresSharp.Model.Texture;

/// <summary>An FTEX section: a Wii U texture, described by its GX2 surface and view and holding its tiled image data.</summary>
/// <remarks>
/// The data is kept as stored: tiled in the layout <see cref="TileMode"/> names. Large textures put the top level in
/// <see cref="Data"/> and the remaining mip levels in <see cref="MipData"/>, and a model may keep either in a separate file
/// (".Tex1" holds the first, ".Tex2" the second), so either can be missing. The registers are kept as read.
/// </remarks>
public sealed class Texture
{
    public const int MipOffsetCount = 13;
    public const int RegisterCount = 5;

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public GX2SurfaceDim Dim { get; set; }

    public uint Width { get; set; }

    public uint Height { get; set; }

    public uint Depth { get; set; }

    public uint MipCount { get; set; }

    public GX2SurfaceFormat Format { get; set; }

    public GX2AAMode AAMode { get; set; }

    public GX2SurfaceUse Use { get; set; }

    public GX2TileMode TileMode { get; set; }

    public uint Swizzle { get; set; }

    public uint Alignment { get; set; }

    public uint Pitch { get; set; }

    public uint[] MipOffsets { get; set; } = new uint[MipOffsetCount];

    public uint ViewMipFirst { get; set; }

    public uint ViewMipCount { get; set; }

    public uint ViewSliceFirst { get; set; }

    public uint ViewSliceCount { get; set; }

    public GX2CompSel CompSelR { get; set; }

    public GX2CompSel CompSelG { get; set; }

    public GX2CompSel CompSelB { get; set; }

    public GX2CompSel CompSelA { get; set; }

    public uint[] Registers { get; set; } = new uint[RegisterCount];

    public byte ArrayLength { get; set; }

    // The sizes the header states for the two images, kept as read: a file that keeps one image elsewhere may still state its size.
    public uint DataSize { get; set; }

    public uint MipDataSize { get; set; }

    public ReadOnlyMemory<byte>? Data { get; set; }

    public ReadOnlyMemory<byte>? MipData { get; set; }

    // Where a Wii U file kept the two images, or -1; kept so that textures whose images overlap or sit where a companion file put them write back the same.
    public int DataPosition { get; set; } = -1;

    public int MipDataPosition { get; set; } = -1;

    public ResDict<UserData> UserData { get; set; } = new();
}
