using BfresSharp.Model.Geometry;

namespace BfresSharp.Model;

/// <summary>An FMDL section: a skeleton, the vertex buffers, shapes and materials drawn with it.</summary>
/// <remarks>
/// Shapes refer to their vertex buffer and material by index into <see cref="VertexBuffers"/> and
/// <see cref="Materials"/>.
/// </remarks>
public sealed class ResModel
{
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public uint Flags { get; set; }

    // The vertex count the file states for the whole model; Switch files before version 9 keep it.
    public uint TotalVertexCount { get; set; }

    public Skeleton.Skeleton Skeleton { get; set; } = new();

    public List<VertexBuffer> VertexBuffers { get; set; } = [];

    public ResDict<Shape> Shapes { get; set; } = new();

    public ResDict<Material.Material> Materials { get; set; } = new();

    public ResDict<UserData> UserData { get; set; } = new();
}
