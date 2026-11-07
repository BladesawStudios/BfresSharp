namespace BfresSharp.Model.Animation;

/// <summary>The flags of a bone animation that say which base values are stored.</summary>
/// <remarks>Bits 3 to 5 of the bone animation flags.</remarks>
[Flags]
public enum BoneAnimFlagsBase : uint
{
    Scale = 1 << 3,
    Rotate = 1 << 4,
    Translate = 1 << 5,
}

/// <summary>The flags of a bone animation that say which components have a curve.</summary>
/// <remarks>Bits 6 to 15 of the bone animation flags.</remarks>
[Flags]
public enum BoneAnimFlagsCurve : uint
{
    ScaleX = 1 << 6,
    ScaleY = 1 << 7,
    ScaleZ = 1 << 8,
    RotateX = 1 << 9,
    RotateY = 1 << 10,
    RotateZ = 1 << 11,
    RotateW = 1 << 12,
    TranslateX = 1 << 13,
    TranslateY = 1 << 14,
    TranslateZ = 1 << 15,
}

/// <summary>The flags of a bone animation that describe its transform.</summary>
/// <remarks>Bits 23 to 27 of the bone animation flags, the same bits a bone uses.</remarks>
[Flags]
public enum BoneAnimFlagsTransform : uint
{
    SegmentScaleCompensate = 1 << 23,
    ScaleUniform = 1 << 24,
    ScaleVolumeOne = 1 << 25,
    RotateZero = 1 << 26,
    TranslateZero = 1 << 27,
    ScaleOne = ScaleVolumeOne | ScaleUniform,
    RotateTranslateZero = RotateZero | TranslateZero,
    Identity = ScaleOne | RotateZero | TranslateZero,
}
