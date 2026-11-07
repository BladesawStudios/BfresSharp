using BfresSharp.Container;
using BfresSharp.Model.Animation;

namespace BfresSharp.Model;

/// <summary>A BFRES archive: the models, animations and embedded files of one resource.</summary>
/// <remarks>
/// Holds the platform and version it was read as, which decide how it is written. <see cref="Reserved"/> keeps the
/// 32 reserved bytes of the Switch header from version 9, and the small header fields (<see cref="Flag"/>,
/// <see cref="Alignment"/>, <see cref="TargetAddressSize"/>, <see cref="ExternalFlags"/>, <see cref="SavedFlags"/>)
/// are kept as read so a file writes back unchanged.
/// </remarks>
public sealed class ResFile
{
    public string Name { get; set; } = "";

    public BfresPlatform Platform { get; set; } = BfresPlatform.Switch;

    public BfresVersion Version { get; set; }

    // The word that starts the buffer info: 0x22, or 0x24 in Switch 2 files.
    public uint BufferInfoKind { get; set; } = 0x22;

    public byte Alignment { get; set; }

    // Wii U files state their data alignment directly rather than as a power of two.
    public uint DataAlignment { get; set; }

    public byte TargetAddressSize { get; set; }

    public ushort Flag { get; set; }

    public byte ExternalFlags { get; set; }

    public byte SavedFlags { get; set; }

    public byte[] Reserved { get; set; } = new byte[32];

    // The names in the order the file's name pool held them, so a file writes its pool back in the same order.
    public List<string> StringOrder { get; set; } = [];

    public ResDict<ResModel> Models { get; set; } = new();

    public ResDict<ExternalFile> ExternalFiles { get; set; } = new();

    // The flag in ExternalFlags of a file whose external entries are 64-bit name hashes, not embedded files.
    public const byte ExternalStringsFlag = 4;

    // Switch 2 keeps the strings its other files leave out as hashes (ExternalBinaryString.bfres): each entry is a
    // name and the 64-bit hash that other files use in its place. Such a file has no ExternalFiles.
    public ResDict<ulong> ExternalStringHashes { get; set; } = new();

    // The names of ExternalStringHashes by hash, for turning the hashes other files hold back into names.
    public Dictionary<ulong, string> NamesByHash()
    {
        var names = new Dictionary<ulong, string>(ExternalStringHashes.Count);
        foreach (KeyValuePair<string, ulong> entry in ExternalStringHashes) names[entry.Value] = entry.Key;
        return names;
    }

    public ResDict<SkeletalAnim> SkeletalAnims { get; set; } = new();

    public ResDict<MaterialAnim> MaterialAnims { get; set; } = new();

    public ResDict<VisibilityAnim> VisibilityAnims { get; set; } = new();

    public ResDict<ShapeAnim> ShapeAnims { get; set; } = new();

    // Wii U files hold their textures and keep each kind of material animation, and the visibility animations of
    // materials, in a dictionary of their own.
    public ResDict<Texture.Texture> Textures { get; set; } = new();

    public ResDict<MaterialAnim> ShaderParamAnims { get; set; } = new();

    public ResDict<MaterialAnim> ColorAnims { get; set; } = new();

    public ResDict<MaterialAnim> TexSrtAnims { get; set; } = new();

    public ResDict<MaterialAnim> TexPatternAnims { get; set; } = new();

    public ResDict<VisibilityAnim> MatVisibilityAnims { get; set; } = new();
}
