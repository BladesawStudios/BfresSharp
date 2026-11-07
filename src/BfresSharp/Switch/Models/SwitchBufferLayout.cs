using BfresSharp.Model;
using BfresSharp.Model.Geometry;

namespace BfresSharp.Switch.Models;

/// <summary>Places the index and vertex data of a file's models in the buffer data section.</summary>
/// <remarks>
/// Index data of every mesh comes first, each padded to 8 bytes, then the streams of every vertex buffer, each aligned
/// to the buffer's GPU alignment. Offsets are relative to the start of the section, which is page aligned in the file.
/// </remarks>
public sealed class SwitchBufferLayout
{
    public const int PageSize = 0x1000;
    private readonly Dictionary<Mesh, uint> _faces = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<VertexBuffer, uint> _streams = new(ReferenceEqualityComparer.Instance);

    public SwitchBufferLayout(ResFile file)
    {
        long position = 0;
        foreach (Mesh mesh in file.Models.Values.SelectMany(model => model.Shapes.Values).SelectMany(shape => shape.Meshes))
        {
            position = Align(position, 8);
            _faces[mesh] = (uint)position;
            position += mesh.IndexData.Length;
        }

        IndexSize = Align(position, 8);
        position = IndexSize;
        foreach (VertexBuffer buffer in file.Models.Values.SelectMany(model => model.VertexBuffers))
        {
            _streams[buffer] = (uint)Align(position, buffer.GpuBufferAlignment);
            foreach (VertexStream stream in buffer.Streams)
            {
                position = Align(position, buffer.GpuBufferAlignment) + stream.Data.Length;
            }
        }

        TotalSize = Align(position, PageSize);
        Models = file.Models;
    }

    public ResDict<ResModel> Models { get; }

    public long IndexSize { get; }

    public long TotalSize { get; }

    public bool IsEmpty => TotalSize == 0;

    public uint FaceOffset(Mesh mesh) => _faces[mesh];

    public uint StreamOffset(VertexBuffer buffer) => _streams[buffer];

    // The index data and then the streams, as they are laid out in the section.
    public byte[] Build()
    {
        var bytes = new byte[TotalSize];
        foreach ((Mesh mesh, uint offset) in _faces) mesh.IndexData.CopyTo(bytes, offset);
        foreach (VertexBuffer buffer in Models.Values.SelectMany(model => model.VertexBuffers))
        {
            long position = _streams[buffer];
            foreach (VertexStream stream in buffer.Streams)
            {
                position = Align(position, buffer.GpuBufferAlignment);
                stream.Data.CopyTo(bytes, position);
                position += stream.Data.Length;
            }
        }

        return bytes;
    }

    private static long Align(long value, int alignment) => alignment <= 1 ? value : (value + alignment - 1) / alignment * alignment;
}
