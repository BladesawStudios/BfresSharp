using BfresSharp.Gx2;

namespace BfresSharp.Model.Geometry;

/// <summary>The width of one index in a mesh's index buffer.</summary>
public enum IndexFormat : uint
{
    Byte = 0,
    UInt16 = 1,
    UInt32 = 2,
}

/// <summary>One level of detail of a shape: how its indices are drawn and the index data itself.</summary>
/// <remarks>
/// The index data is kept as stored, in the byte order of the file it came from. <see cref="IndexCount"/> follows
/// from the data and the format. <see cref="BufferFlags"/> is the GPU access word of the Switch buffer size record.
/// </remarks>
public sealed class Mesh
{
    public GX2PrimitiveType PrimitiveType { get; set; } = GX2PrimitiveType.Triangles;

    public IndexFormat IndexFormat { get; set; } = IndexFormat.UInt16;

    public uint FirstVertex { get; set; }

    public uint BufferFlags { get; set; }

    public List<SubMesh> SubMeshes { get; set; } = [];

    public byte[] IndexData { get; set; } = [];

    public int IndexCount => IndexData.Length / IndexSize;

    public int IndexSize => IndexFormat switch
    {
        IndexFormat.Byte => 1,
        IndexFormat.UInt16 => 2,
        _ => 4,
    };
}
