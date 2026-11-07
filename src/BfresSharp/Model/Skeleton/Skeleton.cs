namespace BfresSharp.Model.Skeleton;

/// <summary>An FSKL section: the bones of a model and the tables that map skinning matrices to them.</summary>
/// <remarks>
/// <see cref="MatrixToBoneList"/> lists the bone behind each skinning matrix, smooth matrices first and then rigid
/// ones; <see cref="InverseModelMatrices"/> holds one inverse matrix per smooth matrix. Mirrored bone indices exist in
/// Switch files from version 9.
/// </remarks>
public sealed class Skeleton
{
    private const uint ScalingMask = 0x3 << 8;
    private const uint RotationMask = 0x7 << 12;

    public uint RawFlags { get; set; } = (uint)SkeletonFlagsScaling.Maya;

    public ResDict<Bone> Bones { get; set; } = new();

    public List<ushort> MatrixToBoneList { get; set; } = [];

    public List<Matrix3x4> InverseModelMatrices { get; set; } = [];

    public ushort[] MirroredBoneIndices { get; set; } = [];

    public SkeletonFlagsScaling FlagsScaling
    {
        get => (SkeletonFlagsScaling)(RawFlags & ScalingMask);
        set => RawFlags = (RawFlags & ~ScalingMask) | (uint)value;
    }

    public SkeletonFlagsRotation FlagsRotation
    {
        get => (SkeletonFlagsRotation)(RawFlags & RotationMask);
        set => RawFlags = (RawFlags & ~RotationMask) | (uint)value;
    }

    public int NumSmoothMatrices => InverseModelMatrices.Count;

    public int NumRigidMatrices => MatrixToBoneList.Count - InverseModelMatrices.Count;

    public IEnumerable<ushort> SmoothIndices => MatrixToBoneList.Take(NumSmoothMatrices);

    public IEnumerable<ushort> RigidIndices => MatrixToBoneList.Skip(NumSmoothMatrices);
}
