namespace BfresSharp.Model.Animation;

/// <summary>The settings flags of a skeletal animation.</summary>
/// <remarks>Bits 0 and 2 of the skeletal animation flags.</remarks>
[Flags]
public enum SkeletalAnimFlags : uint
{
    BakedCurve = 1 << 0,
    Looping = 1 << 2,
}

/// <summary>How a skeletal animation scales bones.</summary>
/// <remarks>Bits 8 and 9 of the skeletal animation flags.</remarks>
public enum SkeletalAnimFlagsScale : uint
{
    None,
    Standard = 1 << 8,
    Maya = 2 << 8,
    Softimage = 3 << 8,
}

/// <summary>How a skeletal animation rotates bones.</summary>
/// <remarks>Bit 12 of the skeletal animation flags.</remarks>
public enum SkeletalAnimFlagsRotate : uint
{
    Quaternion,
    EulerXYZ = 1 << 12,
}
