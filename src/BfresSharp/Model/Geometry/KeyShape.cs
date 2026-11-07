namespace BfresSharp.Model.Geometry;

/// <summary>A blend shape: which vertex attributes it replaces and the offsets of the target index runs.</summary>
public sealed class KeyShape
{
    public const int IndexCount = 20;
    public const int OffsetCount = 4;

    public byte[] TargetAttribIndices { get; set; } = new byte[IndexCount];

    public byte[] TargetAttribIndexOffsets { get; set; } = new byte[OffsetCount];
}
