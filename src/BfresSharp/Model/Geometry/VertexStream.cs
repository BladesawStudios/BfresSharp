namespace BfresSharp.Model.Geometry;

/// <summary>One interleaved stream of a vertex buffer: the bytes of every vertex and the distance between vertices.</summary>
public sealed class VertexStream
{
    public ushort Stride { get; set; }

    public uint GpuAccessFlags { get; set; }

    // Wii U files hold this many copies of the data back to back; zero is how some files state one copy.
    public ushort BufferCount { get; set; } = 1;

    public byte[] Data { get; set; } = [];
}
