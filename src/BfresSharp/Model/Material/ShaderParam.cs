namespace BfresSharp.Model.Material;

/// <summary>The type of a shader parameter, which fixes how many bytes of the material's parameter block it takes.</summary>
public enum ShaderParamType : byte
{
    Bool, Bool2, Bool3, Bool4,
    Int, Int2, Int3, Int4,
    UInt, UInt2, UInt3, UInt4,
    Float, Float2, Float3, Float4,
    Reserved2, Float2x2, Float2x3, Float2x4,
    Reserved3, Float3x2, Float3x3, Float3x4,
    Reserved4, Float4x2, Float4x3, Float4x4,
    Srt2D, Srt3D, TexSrt, TexSrtEx,
}

/// <summary>A shader parameter of a material: where its value sits in the material's parameter block.</summary>
/// <remarks>
/// The values themselves live in <see cref="Material.ShaderParamData"/>; <see cref="ShaderParamValues"/> reads and
/// writes them. Version 10 Switch files may name a parameter by hash alone, kept in <see cref="NameHash"/>.
/// <see cref="PaddingLength"/> counts bytes a Wii U file reserves after the value.
/// </remarks>
public sealed class ShaderParam
{
    public string Name { get; set; } = "";

    public ulong NameHash { get; set; }

    public ShaderParamType Type { get; set; }

    public ushort DataOffset { get; set; }

    public int UniformOffset { get; set; } = -1;

    public uint CallbackPointer { get; set; }

    public ushort DependedIndex { get; set; }

    public ushort DependIndex { get; set; }

    public int PaddingLength { get; set; }

    public int DataSize => SizeOf(Type);

    public static int SizeOf(ShaderParamType type)
    {
        if (type <= ShaderParamType.Float4) return 4 * (((int)type & 3) + 1);
        if (type <= ShaderParamType.Float4x4)
        {
            int columns = ((int)type & 3) + 1;
            int rows = (((int)type - (int)ShaderParamType.Reserved2) >> 2) + 2;
            return 4 * columns * rows;
        }

        return type switch
        {
            ShaderParamType.Srt2D => 20,
            ShaderParamType.Srt3D => 36,
            ShaderParamType.TexSrt => 24,
            ShaderParamType.TexSrtEx => 28,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }
}
