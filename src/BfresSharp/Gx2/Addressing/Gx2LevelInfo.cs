namespace BfresSharp.Gx2.Addressing;

/// <summary>The padded size, alignments and tile mode of one mip level of a GX2 surface.</summary>
/// <remarks>
/// Pitch and height are in elements: pixels, or 4x4 blocks for block compressed formats. Depth is the slice count after
/// padding, Size the bytes of the whole level (every slice) and BitsPerElement the bits of one element. The tile mode is
/// the one the level is stored in, which can differ from the surface's mode for small mip levels. ElementsWide and
/// ElementsHigh are the unpadded size of the picture in elements, the part of the pitch and height that holds pixels.
/// </remarks>
public readonly record struct Gx2LevelInfo(
    uint Pitch,
    uint Height,
    uint Depth,
    uint Size,
    uint BaseAlign,
    uint PitchAlign,
    uint HeightAlign,
    uint DepthAlign,
    uint TileMode,
    uint BitsPerElement,
    uint Samples = 1,
    uint ElementsWide = 0,
    uint ElementsHigh = 0);
