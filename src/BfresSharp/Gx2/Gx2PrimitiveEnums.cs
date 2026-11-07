#pragma warning disable 1591

namespace BfresSharp.Gx2;

/// <summary>
/// Represents the type in which vertex indices are stored.
/// </summary>
public enum GX2IndexFormat : uint
{
    UInt16LittleEndian = 0,
    UInt32LittleEndian = 1,
    UInt16 = 4,
    UInt32 = 9,
}

/// <summary>
/// Represents the type of primitives to draw.
/// </summary>
public enum GX2PrimitiveType : uint
{
    /// <summary>
    /// Requires at least 1 element and 1 more to draw another primitive.
    /// </summary>
    Points = 0x01,

    /// <summary>
    /// Requires at least 2 elements and 2 more to draw another primitive.
    /// </summary>
    Lines = 0x02,

    /// <summary>
    /// Requires at least 2 elements and 1 more to draw another primitive.
    /// </summary>
    LineStrip = 0x03,

    /// <summary>
    /// Requires at least 3 elements and 3 more to draw another primitive.
    /// </summary>
    Triangles = 0x04,

    /// <summary>
    /// Requires at least 3 elements and 1 more to draw another primitive.
    /// </summary>
    TriangleFan = 0x05,

    /// <summary>
    /// Requires at least 3 elements and 1 more to draw another primitive.
    /// </summary>
    TriangleStrip = 0x06,

    /// <summary>
    /// Requires at least 4 elements and 4 more to draw another primitive.
    /// </summary>
    LinesAdjacency = 0x0A,

    /// <summary>
    /// Requires at least 4 elements and 1 more to draw another primitive.
    /// </summary>
    LineStripAdjacency = 0x0B,

    /// <summary>
    /// Requires at least 6 elements and 6 more to draw another primitive.
    /// </summary>
    TrianglesAdjacency = 0x0C,

    /// <summary>
    /// Requires at least 6 elements and 2 more to draw another primitive.
    /// </summary>
    TriangleStripAdjacency = 0x0D,

    /// <summary>
    /// Requires at least 3 elements and 3 more to draw another primitive.
    /// </summary>
    Rects = 0x11,

    /// <summary>
    /// Requires at least 2 elements and 1 more to draw another primitive.
    /// </summary>
    LineLoop = 0x12,

    /// <summary>
    /// Requires at least 4 elements and 4 more to draw another primitive.
    /// </summary>
    Quads = 0x13,

    /// <summary>
    /// Requires at least 4 elements and 2 more to draw another primitive.
    /// </summary>
    QuadStrip = 0x14,

    /// <summary>
    /// Requires at least 2 elements and 2 more to draw another primitive.
    /// </summary>
    TessellateLines = 0x82,

    /// <summary>
    /// Requires at least 2 elements and 1 more to draw another primitive.
    /// </summary>
    TessellateLineStrip = 0x83,

    /// <summary>
    /// Requires at least 3 elements and 3 more to draw another primitive.
    /// </summary>
    TessellateTriangles = 0x84,

    /// <summary>
    /// Requires at least 3 elements and 1 more to draw another primitive.
    /// </summary>
    TessellateTriangleStrip = 0x86,

    /// <summary>
    /// Requires at least 4 elements and 4 more to draw another primitive.
    /// </summary>
    TessellateQuads = 0x93,

    /// <summary>
    /// Requires at least 4 elements and 2 more to draw another primitive.
    /// </summary>
    TessellateQuadStrip = 0x94
}
