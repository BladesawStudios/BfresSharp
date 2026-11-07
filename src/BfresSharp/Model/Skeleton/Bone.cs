using System.Numerics;

namespace BfresSharp.Model.Skeleton;

/// <summary>A bone of a skeleton: its place in the hierarchy, its matrix indices and its local transform.</summary>
/// <remarks>
/// The flags word is kept whole. Each flag group has a property that reads and writes only its own bits, and
/// <see cref="Visible"/> clears the bit when set to false, which the library this replaces did not.
/// <see cref="InverseMatrix"/> is only stored by Wii U files older than version 3.4.
/// </remarks>
public sealed class Bone
{
    private const uint VisibleMask = 0x1;
    private const uint RotationMask = 0x1000;
    private const uint BillboardMask = 0x7 << 16;
    private const uint TransformMask = 0xF << 24;
    private const uint CumulativeMask = 0xFu << 28;

    public string Name { get; set; } = "";

    public uint RawFlags { get; set; }

    public short ParentIndex { get; set; } = -1;

    public short SmoothMatrixIndex { get; set; } = -1;

    public short RigidMatrixIndex { get; set; } = -1;

    public short BillboardIndex { get; set; } = -1;

    public Vector3 Scale { get; set; } = Vector3.One;

    public Vector4 Rotation { get; set; } = new(0, 0, 0, 1);

    public Vector3 Position { get; set; }

    public Matrix3x4? InverseMatrix { get; set; }

    public ResDict<UserData> UserData { get; set; } = new();

    public bool Visible
    {
        get => (RawFlags & VisibleMask) != 0;
        set => RawFlags = value ? RawFlags | VisibleMask : RawFlags & ~VisibleMask;
    }

    public BoneFlagsRotation FlagsRotation
    {
        get => (BoneFlagsRotation)(RawFlags & RotationMask);
        set => RawFlags = (RawFlags & ~RotationMask) | (uint)value;
    }

    public BoneFlagsBillboard FlagsBillboard
    {
        get => (BoneFlagsBillboard)(RawFlags & BillboardMask);
        set => RawFlags = (RawFlags & ~BillboardMask) | (uint)value;
    }

    public BoneFlagsTransform FlagsTransform
    {
        get => (BoneFlagsTransform)(RawFlags & TransformMask);
        set => RawFlags = (RawFlags & ~TransformMask) | (uint)value;
    }

    public BoneFlagsTransformCumulative FlagsTransformCumulative
    {
        get => (BoneFlagsTransformCumulative)(RawFlags & CumulativeMask);
        set => RawFlags = (RawFlags & ~CumulativeMask) | (uint)value;
    }

    public bool TransformIdentity => FlagsTransform.HasFlag(BoneFlagsTransform.Identity);

    public bool TransformScaleOne => FlagsTransform.HasFlag(BoneFlagsTransform.ScaleOne);

    public bool TransformScaleUniform => FlagsTransform.HasFlag(BoneFlagsTransform.ScaleUniform);

    public bool TransformScaleVolumeOne => FlagsTransform.HasFlag(BoneFlagsTransform.ScaleVolumeOne);

    public bool TransformRotateZero => FlagsTransform.HasFlag(BoneFlagsTransform.RotateZero);

    public bool TransformTranslateZero => FlagsTransform.HasFlag(BoneFlagsTransform.TranslateZero);

    public bool TransformRotateTranslateZero => FlagsTransform.HasFlag(BoneFlagsTransform.RotateTranslateZero);

    public bool TransformCumulativeIdentity => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.Identity);

    public bool TransformCumulativeScaleOne => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.ScaleOne);

    public bool TransformCumulativeScaleUniform => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.ScaleUniform);

    public bool TransformCumulativeScaleVolumeOne => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.ScaleVolumeOne);

    public bool TransformCumulativeRotateZero => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.RotateZero);

    public bool TransformCumulativeTranslateZero => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.TranslateZero);

    public bool TransformCumulativeRotateTranslateZero => FlagsTransformCumulative.HasFlag(BoneFlagsTransformCumulative.RotateTranslateZero);
}
