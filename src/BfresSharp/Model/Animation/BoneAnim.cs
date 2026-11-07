namespace BfresSharp.Model.Animation;

/// <summary>The animation of one bone: optional base values and the curves that move it.</summary>
/// <remarks>
/// The flags word is kept whole; <see cref="FlagsBase"/>, <see cref="FlagsCurve"/> and <see cref="FlagsTransform"/>
/// read and write the bits they own. <see cref="Reserved"/> holds the two unused 8-byte words of version 9 and later
/// layouts so they write back as read.
/// </remarks>
public sealed class BoneAnim
{
    private const uint BaseMask = 0b111000;
    private const uint CurveMask = 0b1111_1111_1100_0000;
    private const uint TransformMask = 0b0000_1111_1000_0000_0000_0000_0000_0000;

    public string Name { get; set; } = "";

    public uint Flags { get; set; }

    public BoneAnimFlagsBase FlagsBase
    {
        get => (BoneAnimFlagsBase)(Flags & BaseMask);
        set => Flags = (Flags & ~BaseMask) | (uint)value;
    }

    public BoneAnimFlagsCurve FlagsCurve
    {
        get => (BoneAnimFlagsCurve)(Flags & CurveMask);
        set => Flags = (Flags & ~CurveMask) | (uint)value;
    }

    public BoneAnimFlagsTransform FlagsTransform
    {
        get => (BoneAnimFlagsTransform)(Flags & TransformMask);
        set => Flags = (Flags & ~TransformMask) | (uint)value;
    }

    public byte BeginRotate { get; set; }

    public byte BeginTranslate { get; set; }

    public byte BeginBaseTranslate { get; set; }

    public int BeginCurve { get; set; }

    public BoneAnimData? BaseData { get; set; }

    public List<AnimCurve> Curves { get; set; } = [];

    public ulong[] Reserved { get; set; } = new ulong[2];
}
