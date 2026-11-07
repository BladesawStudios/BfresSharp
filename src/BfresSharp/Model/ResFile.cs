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

    public byte Alignment { get; set; }

    public byte TargetAddressSize { get; set; }

    public ushort Flag { get; set; }

    public byte ExternalFlags { get; set; }

    public byte SavedFlags { get; set; }

    public byte[] Reserved { get; set; } = new byte[32];

    // The names in the order the file's name pool held them, so a file writes its pool back in the same order.
    public List<string> StringOrder { get; set; } = [];

    public ResDict<SkeletalAnim> SkeletalAnims { get; set; } = new();

    public ResDict<VisibilityAnim> VisibilityAnims { get; set; } = new();
}
