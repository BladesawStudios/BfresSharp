namespace BfresSharp.Model.Material;

public enum TexWrap : byte
{
    Repeat,
    Mirror,
    Clamp,
    ClampToEdge,
    MirrorOnce,
    MirrorOnceClampToEdge,
}

public enum TexCompare : byte
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always,
}

public enum TexBorder : byte
{
    White,
    Transparent,
    Opaque,
}

public enum TexFilter : byte
{
    None = 0,
    Points = 1,
    Linear = 2,
}

public enum TexAnisotropy : byte
{
    Ratio1 = 0x1,
    Ratio2 = 0x2,
    Ratio4 = 0x4,
    Ratio8 = 0x8,
    Ratio16 = 0x10,
}

/// <summary>How a material samples one of its textures.</summary>
/// <remarks>
/// The values are the Switch ones; the Wii U codec converts to and from the GX2 sampler words. The three filters
/// are packed into one filter word by the Switch layout, so <see cref="FilterFlags"/> keeps unknown bits.
/// </remarks>
public sealed class Sampler
{
    private const ushort MipMask = 0x3;
    private const ushort ExpandMask = 0xC;
    private const ushort ShrinkMask = 0x30;

    public TexWrap WrapU { get; set; } = TexWrap.Repeat;

    public TexWrap WrapV { get; set; } = TexWrap.Repeat;

    public TexWrap WrapW { get; set; } = TexWrap.Clamp;

    public TexCompare CompareFunc { get; set; } = TexCompare.Never;

    public TexBorder Border { get; set; } = TexBorder.White;

    public TexAnisotropy Anisotropy { get; set; } = TexAnisotropy.Ratio1;

    public ushort FilterFlags { get; set; } = 42;

    public float MinLod { get; set; }

    public float MaxLod { get; set; } = 13;

    public float LodBias { get; set; }

    public TexFilter Mip
    {
        get => (TexFilter)(FilterFlags & MipMask);
        set => FilterFlags = (ushort)((FilterFlags & ~MipMask) | (ushort)value);
    }

    public TexFilter Expand
    {
        get => (TexFilter)((FilterFlags & ExpandMask) >> 2);
        set => FilterFlags = (ushort)((FilterFlags & ~ExpandMask) | ((ushort)value << 2));
    }

    public TexFilter Shrink
    {
        get => (TexFilter)((FilterFlags & ShrinkMask) >> 4);
        set => FilterFlags = (ushort)((FilterFlags & ~ShrinkMask) | ((ushort)value << 4));
    }
}
