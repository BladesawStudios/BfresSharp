using System.Numerics;

namespace BfresSharp.Model.Geometry;

/// <summary>An FSHP section: a drawable piece of a model bound to one material, one bone and one vertex buffer.</summary>
/// <remarks>
/// Bounding data comes in two shapes. Switch files store one box per mesh and sub mesh; Wii U files from version
/// 4.5 may store a tree of <see cref="SubMeshBoundingNodes"/> with an index list. <see cref="BoundingRadii"/> is the
/// per-mesh or per-bone radius vector list of Switch version 10.
/// </remarks>
public sealed class Shape
{
    public string Name { get; set; } = "";

    public uint Flags { get; set; }

    public ushort MaterialIndex { get; set; }

    public ushort BoneIndex { get; set; }

    public ushort VertexBufferIndex { get; set; }

    public byte VertexSkinCount { get; set; }

    public byte TargetAttribCount { get; set; }

    public List<float> Radii { get; set; } = [];

    public List<Vector4> BoundingRadii { get; set; } = [];

    public List<Mesh> Meshes { get; set; } = [];

    public List<ushort> SkinBoneIndices { get; set; } = [];

    public ResDict<KeyShape> KeyShapes { get; set; } = new();

    public List<Bounding> SubMeshBoundings { get; set; } = [];

    public List<BoundingNode> SubMeshBoundingNodes { get; set; } = [];

    public List<ushort> SubMeshBoundingIndices { get; set; } = [];
}
