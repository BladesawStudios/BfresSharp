using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes shader parameters in the Switch layout, which is 32 bytes.</summary>
/// <remarks>
/// An unused callback pointer, the name pointer, the type, the value size, the offset of the value in the parameter
/// block, the uniform offset, the two dependency indices and four unused bytes.
/// </remarks>
public static class ShaderParamCodec
{
    public const int Size = 32;

    public static ShaderParam Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        return new ShaderParam
        {
            Name = reader.String(at + 8) ?? "",
            Type = (ShaderParamType)view.U8(at + 0x10),
            DataOffset = view.U16(at + 0x12),
            UniformOffset = view.I32(at + 0x14),
            DependedIndex = view.U16(at + 0x18),
            DependIndex = view.U16(at + 0x1A),
        };
    }

    public static void Write(SwitchWriter writer, ShaderParam param)
    {
        writer.Unlisted();
        writer.StringPointer(param.Name);
        writer.Out.U8((byte)param.Type).U8((byte)param.DataSize).U16(param.DataOffset).U32((uint)param.UniformOffset)
            .U16(param.DependedIndex).U16(param.DependIndex).U32(0);
    }
}
