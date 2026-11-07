namespace BfresSharp.Model.Animation;

/// <summary>An FBVS section: the animation of bone visibility, one flag and one name per animated bone.</summary>
/// <remarks>
/// <see cref="BaseData"/> holds the visibility each named bone starts with, and <see cref="Curves"/> animate it. The two
/// spare words of the header are kept as read so the section writes back unchanged.
/// </remarks>
public sealed class VisibilityAnim
{
    private const ushort BakedMask = 1 << 0;
    private const ushort LoopMask = 1 << 2;

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public ushort Flags { get; set; }

    public ushort Pad { get; set; }

    public ushort Spare { get; set; }

    public int FrameCount { get; set; }

    public uint BakedSize { get; set; }

    public bool Loop
    {
        get => (Flags & LoopMask) != 0;
        set => Flags = (ushort)(value ? Flags | LoopMask : Flags & ~LoopMask);
    }

    public bool Baked
    {
        get => (Flags & BakedMask) != 0;
        set => Flags = (ushort)(value ? Flags | BakedMask : Flags & ~BakedMask);
    }

    public ushort[] BindIndices { get; set; } = [];

    public List<string> Names { get; set; } = [];

    public List<AnimCurve> Curves { get; set; } = [];

    public bool[] BaseData { get; set; } = [];

    public ResDict<UserData> UserData { get; set; } = new();
}
