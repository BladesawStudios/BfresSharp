namespace BfresSharp.Model.Animation;

/// <summary>An FSKA section: the animation of a skeleton, one <see cref="BoneAnim"/> per animated bone.</summary>
/// <remarks>
/// The flags word is kept whole; <see cref="Loop"/>, <see cref="FlagsScale"/> and <see cref="FlagsRotate"/> read and
/// write the bits they own. <see cref="BindIndices"/> gives, for each bone animation, the index of the bone of the
/// bound skeleton it drives.
/// </remarks>
public sealed class SkeletalAnim
{
    private const uint SettingsMask = 0b1111;
    private const uint ScaleMask = 0b11 << 8;
    private const uint RotateMask = 0b111 << 12;

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public uint Flags { get; set; }

    public SkeletalAnimFlags FlagsAnimSettings
    {
        get => (SkeletalAnimFlags)(Flags & SettingsMask);
        set => Flags = (Flags & ~SettingsMask) | (uint)value;
    }

    public SkeletalAnimFlagsScale FlagsScale
    {
        get => (SkeletalAnimFlagsScale)(Flags & ScaleMask);
        set => Flags = (Flags & ~ScaleMask) | (uint)value;
    }

    public SkeletalAnimFlagsRotate FlagsRotate
    {
        get => (SkeletalAnimFlagsRotate)(Flags & RotateMask);
        set => Flags = (Flags & ~RotateMask) | (uint)value;
    }

    public bool Loop
    {
        get => FlagsAnimSettings.HasFlag(SkeletalAnimFlags.Looping);
        set => FlagsAnimSettings = value ? FlagsAnimSettings | SkeletalAnimFlags.Looping : FlagsAnimSettings & ~SkeletalAnimFlags.Looping;
    }

    public bool Baked
    {
        get => FlagsAnimSettings.HasFlag(SkeletalAnimFlags.BakedCurve);
        set => FlagsAnimSettings = value ? FlagsAnimSettings | SkeletalAnimFlags.BakedCurve : FlagsAnimSettings & ~SkeletalAnimFlags.BakedCurve;
    }

    public int FrameCount { get; set; }

    public uint BakedSize { get; set; }

    public List<BoneAnim> BoneAnims { get; set; } = [];

    public ushort[] BindIndices { get; set; } = [];

    public ResDict<UserData> UserData { get; set; } = new();
}
