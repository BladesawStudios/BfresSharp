using BfresSharp.Model.Material;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes shader assignments, shader parameters and samplers in the Wii U layout.</summary>
/// <remarks>
/// An assignment is 28 bytes: the archive and model names, the revision, the counts and three dictionaries of names. A
/// parameter is 20 bytes (from version 3.4): the type, its size with padding, the data offset, the uniform offset, a
/// callback pointer, the depended and depend indices and the name. A sampler is 24 bytes: three GX2 registers, a handle,
/// the name and the index.
/// </remarks>
public static class ShaderCodec
{
    public const int ParamSize = 0x14;
    public const int SamplerSize = 0x18;

    public static ShaderAssign ReadAssign(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new ShaderAssign
        {
            ShaderArchiveName = reader.String(at) ?? "",
            ShadingModelName = reader.String(at + 4) ?? "",
            Revision = view.U32(at + 8),
            AttribAssigns = reader.ReadNameDict(at + 0x10),
            SamplerAssigns = reader.ReadNameDict(at + 0x14),
            ShaderOptions = reader.ReadNameDict(at + 0x18),
        };
    }

    public static void WriteAssign(WiiUWriter writer, ShaderAssign assign)
    {
        writer.Name(assign.ShaderArchiveName);
        writer.Name(assign.ShadingModelName);
        writer.Out.U32(assign.Revision).U8((byte)assign.AttribAssigns.Count).U8((byte)assign.SamplerAssigns.Count).U16((ushort)assign.ShaderOptions.Count);
        writer.NameDict(assign.AttribAssigns);
        writer.NameDict(assign.SamplerAssigns);
        writer.NameDict(assign.ShaderOptions);
        writer.Place(assign.AttribAssigns, assign.SamplerAssigns, assign.ShaderOptions);
    }

    public static ShaderParam ReadParam(WiiUReader reader, long at)
    {
        var view = reader.View;
        var param = new ShaderParam
        {
            Name = reader.String(at + 0x10) ?? "",
            Type = (ShaderParamType)view.U8(at),
            DataOffset = view.U16(at + 2),
            UniformOffset = view.I32(at + 4),
            CallbackPointer = view.U32(at + 8),
            DependedIndex = view.U16(at + 0xC),
            DependIndex = view.U16(at + 0xE),
        };
        param.PaddingLength = Math.Max(0, view.U8(at + 1) - param.DataSize);
        return param;
    }

    public static void WriteParam(WiiUWriter writer, ShaderParam param)
    {
        writer.Out.U8((byte)param.Type).U8((byte)(param.DataSize + param.PaddingLength)).U16(param.DataOffset).U32((uint)param.UniformOffset)
            .U32(param.CallbackPointer).U16(param.DependedIndex).U16(param.DependIndex);
        writer.Name(param.Name);
    }

    public static Sampler ReadSampler(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new Sampler { Registers = [view.U32(at), view.U32(at + 4), view.U32(at + 8)] };
    }

    public static void WriteSampler(WiiUWriter writer, Sampler sampler, string name, int index)
    {
        uint[] registers = sampler.Registers.Length == 3 ? sampler.Registers : [0, 0, 0];
        writer.Out.U32(registers[0]).U32(registers[1]).U32(registers[2]).U32(0);
        writer.Name(name);
        writer.Out.U8((byte)index).Zeros(3);
    }
}
