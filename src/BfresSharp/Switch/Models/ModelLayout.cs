using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>The offsets of the fields of an FMDL section, which versions 9 and later arrange differently.</summary>
/// <remarks>
/// Offsets are from the first byte after the magic and the flags or linked block. Version 9 pads the material
/// pointers and version 10 puts the shader assign array after them. The counts follow the user data pointers and an
/// unused word; Switch 2 models end with a 32-byte block, which holds the file's buffer info when it has buffers.
/// </remarks>
public sealed class ModelLayout(SwitchProfile profile)
{
    private readonly bool _modern = profile.Modern;
    private readonly bool _v10 = profile.Version.Major >= 10;

    public const int ExtensionSize = 0x20;
    public const int Skeleton = 0x10;
    public const int VertexBuffers = 0x18;
    public const int Shapes = 0x20;
    public const int ShapeDict = 0x28;
    public const int Materials = 0x30;

    public int MaterialDict => profile.Version.Major == 9 ? 0x40 : 0x38;

    public int ShaderAssigns => 0x40;

    public bool HasShaderAssigns => _v10;

    public int UserArray => _modern ? 0x48 : 0x40;

    public int UserDict => UserArray + 8;

    public int Counts => _modern ? 0x60 : 0x58;

    public int ShaderAssignCount => Counts + 6;

    public int UserCount => _modern ? Counts + 8 : Counts + 6;

    public int Fields => _modern ? 8 : 0x10;

    public int Extension => Counts + 0x10;

    public bool HasExtension => _v10 && (profile.Version.Minor >= 4 || profile.Version.Major > 10);

    public int Size => Fields + Extension + (HasExtension ? ExtensionSize : 0);
}
