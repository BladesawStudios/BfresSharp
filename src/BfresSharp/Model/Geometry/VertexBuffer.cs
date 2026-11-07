namespace BfresSharp.Model.Geometry;

/// <summary>An FVTX section: the vertex attributes of a model and the streams that hold their data.</summary>
/// <remarks>
/// <see cref="GpuBufferAlignment"/> is only stored by Switch files of version 10 and later; earlier ones align
/// stream data to 8 bytes.
/// </remarks>
public sealed class VertexBuffer
{
    // Some Switch 2 shapes point at a vertex buffer record that is all zeros.
    public bool IsBlank { get; set; }

    public uint VertexCount { get; set; }

    public byte VertexSkinCount { get; set; }

    public ushort GpuBufferAlignment { get; set; } = 8;

    public uint Flags { get; set; }

    public ResDict<VertexAttrib> Attributes { get; set; } = new();

    public List<VertexStream> Streams { get; set; } = [];
}
