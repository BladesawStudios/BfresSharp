namespace BfresSharp.Model.Geometry;

/// <summary>The geometry of a shape that Switch 2 files store as one block of clustered data instead of meshes.</summary>
/// <remarks>
/// Such a shape has no meshes and an empty vertex buffer. <see cref="Data"/> is the whole block as stored in the
/// buffer data and <see cref="BufferFlags"/> the GPU access word of its size record. <see cref="Header"/> holds the
/// 32 bytes after the pointers of the block, of which the byte at offset 0x15 counts the entries of the two tables.
/// </remarks>
public sealed class ClusterMesh
{
    public const int HeaderLength = 32;
    public const int CountIndex = 0x15;

    public uint BufferFlags { get; set; }

    public byte[] Header { get; set; } = new byte[HeaderLength];

    public byte[] Data { get; set; } = [];

    public uint[] Offsets { get; set; } = [];

    public uint[] Sizes { get; set; } = [];
}
