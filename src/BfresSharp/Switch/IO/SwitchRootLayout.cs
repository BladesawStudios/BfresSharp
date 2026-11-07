namespace BfresSharp.Switch.IO;

/// <summary>The offsets of the fields of a Switch file header, which version 9 and later shift by 0x20.</summary>
/// <remarks>
/// After the common 0x20 bytes come the name, the model array and dictionary pointers, 32 reserved bytes (version 9 and
/// later), then an array pointer and a dictionary pointer for each kind of animation, the memory pool, buffer info,
/// external file array and dictionary, a padding word, the string table pointer and its size, and the section counts.
/// </remarks>
public sealed class SwitchRootLayout(SwitchProfile profile)
{
    private readonly int _shift = profile.Modern ? 0x20 : 0;

    public const int Name = 0x20;
    public const int Models = 0x28;
    public const int ModelDict = 0x30;
    public const int ReservedLength = 32;

    public int Reserved => 0x38;

    public int SkeletalAnims => 0x38 + _shift;

    public int MaterialAnims => 0x48 + _shift;

    public int BoneVisibilityAnims => 0x58 + _shift;

    public int ShapeAnims => 0x68 + _shift;

    public int SceneAnims => 0x78 + _shift;

    public int MemoryPool => 0x88 + _shift;

    public int BufferInfo => 0x90 + _shift;

    public int ExternalFiles => 0x98 + _shift;

    public int StringTable => 0xB0 + _shift;

    public int StringPoolSize => 0xB8 + _shift;

    // The count of models; the counts of the other kinds follow it in the order of the fields above.
    public int Counts => 0xBC + _shift;

    // Version 9 and later hold two extra counts between the model count and the animation counts.
    public int AnimCounts => Counts + 2 + (profile.Modern ? 4 : 0);

    public int Flags => AnimCounts + 12;

    public int Size => profile.Modern ? 0xF0 : 0xD0;
}
