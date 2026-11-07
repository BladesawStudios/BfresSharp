namespace BfresSharp.Model.Geometry;

/// <summary>A node of the binary search tree that groups the sub meshes of a shape for culling.</summary>
public sealed class BoundingNode
{
    public ushort LeftChildIndex { get; set; }

    public ushort RightChildIndex { get; set; }

    public ushort Unknown { get; set; }

    public ushort NextSibling { get; set; }

    public ushort SubMeshIndex { get; set; }

    public ushort SubMeshCount { get; set; }
}
