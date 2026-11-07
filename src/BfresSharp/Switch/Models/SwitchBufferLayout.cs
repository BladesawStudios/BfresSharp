using BfresSharp.Container;
using BfresSharp.Model;
using BfresSharp.Model.Geometry;

namespace BfresSharp.Switch.Models;

/// <summary>Places the index and vertex data of a file's models in the buffer data section.</summary>
/// <remarks>
/// Index data of every mesh comes first, then the cluster blocks of shapes that have them, each padded to 8 bytes (256 in Switch 2 files), then the streams of every vertex buffer, each aligned
/// to the buffer's GPU alignment. Offsets are relative to the start of the section, which is page aligned in the file.
/// </remarks>
public sealed class SwitchBufferLayout
{
    public const int PageSize = 0x1000;
    private const int Switch2FaceAlignment = 0x100;
    private const int ClusterAlignment = 0x100;
    private readonly Dictionary<Mesh, uint> _faces = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ClusterMesh, uint> _clusters = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<VertexBuffer, uint> _streams = new(ReferenceEqualityComparer.Instance);

    public SwitchBufferLayout(ResFile file)
    {
        long position = 0;
        int faceAlignment = file.Platform == BfresPlatform.Switch2 ? Switch2FaceAlignment : 8;
        foreach (Mesh mesh in file.Models.Values.SelectMany(model => model.Shapes.Values).SelectMany(shape => shape.Meshes))
        {
            position = Align(position, faceAlignment);
            _faces[mesh] = (uint)position;
            position += mesh.IndexData.Length;
        }

        IndexSize = Align(position, faceAlignment);
        foreach (ClusterMesh cluster in file.Models.Values.SelectMany(model => model.Shapes.Values).Select(shape => shape.Cluster).OfType<ClusterMesh>())
        {
            position = Align(Math.Max(position, IndexSize), ClusterAlignment);
            _clusters[cluster] = (uint)position;
            position += cluster.Data.Length;
        }

        if (_clusters.Count > 0) IndexSize = Align(position, faceAlignment);
        position = IndexSize;
        foreach (VertexBuffer buffer in file.Models.Values.SelectMany(model => model.VertexBuffers).Where(buffer => !buffer.IsBlank))
        {
            _streams[buffer] = (uint)Align(position, buffer.GpuBufferAlignment);
            foreach (VertexStream stream in buffer.Streams)
            {
                position = Align(position, buffer.GpuBufferAlignment) + stream.Data.Length;
            }
        }

        TotalSize = Align(position, PageSize);
        SplitAt = _streams.Count > 0 ? _streams.Values.Min() : IndexSize;
        Models = file.Models;
    }

    public ResDict<ResModel> Models { get; }

    public long IndexSize { get; }

    public long TotalSize { get; }

    // Where the vertex data starts, which is where the relocation table's index data section ends.
    public long SplitAt { get; }

    public bool IsEmpty => TotalSize == 0;

    public uint FaceOffset(Mesh mesh) => _faces[mesh];

    public uint StreamOffset(VertexBuffer buffer) => _streams.GetValueOrDefault(buffer);

    public uint ClusterOffset(ClusterMesh cluster) => _clusters[cluster];

    // Whether the section holds cluster data, which files keep apart from index and vertex data.
    public bool HasClusters => _clusters.Count > 0;

    // The index data and then the streams, as they are laid out in the section.
    public void BuildInto(Span<byte> bytes)
    {
        foreach ((Mesh mesh, uint offset) in _faces) mesh.IndexData.CopyTo(bytes[(int)offset..]);
        foreach ((ClusterMesh cluster, uint offset) in _clusters) cluster.Data.CopyTo(bytes[(int)offset..]);
        foreach (VertexBuffer buffer in Models.Values.SelectMany(model => model.VertexBuffers).Where(buffer => !buffer.IsBlank))
        {
            long position = _streams[buffer];
            foreach (VertexStream stream in buffer.Streams)
            {
                position = Align(position, buffer.GpuBufferAlignment);
                stream.Data.CopyTo(bytes[(int)position..]);
                position += stream.Data.Length;
            }
        }
    }

    private static long Align(long value, int alignment) => alignment <= 1 ? value : (value + alignment - 1) / alignment * alignment;
}
