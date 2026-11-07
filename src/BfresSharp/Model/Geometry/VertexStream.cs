namespace BfresSharp.Model.Geometry;

/// <summary>One interleaved stream of a vertex buffer: the bytes of every vertex and the distance between vertices.</summary>
public sealed class VertexStream
{
    public ushort Stride { get; set; }

    public uint GpuAccessFlags { get; set; }

    public byte[] Data { get; set; } = [];
}
