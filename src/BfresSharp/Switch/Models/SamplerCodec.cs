using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes samplers in the Switch layout, which is 32 bytes.</summary>
/// <remarks>
/// Six bytes of wrap, compare, border and anisotropy values, the packed filter word, the three LOD floats and 12 unused
/// bytes.
/// </remarks>
public static class SamplerCodec
{
    public const int Size = 32;

    public static Sampler Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        return new Sampler
        {
            WrapU = (TexWrap)view.U8(at),
            WrapV = (TexWrap)view.U8(at + 1),
            WrapW = (TexWrap)view.U8(at + 2),
            CompareFunc = (TexCompare)view.U8(at + 3),
            Border = (TexBorder)view.U8(at + 4),
            Anisotropy = (TexAnisotropy)view.U8(at + 5),
            FilterFlags = view.U16(at + 6),
            MinLod = view.F32(at + 8),
            MaxLod = view.F32(at + 0xC),
            LodBias = view.F32(at + 0x10),
        };
    }

    public static void Write(SwitchWriter writer, Sampler sampler)
    {
        writer.Out.U8((byte)sampler.WrapU).U8((byte)sampler.WrapV).U8((byte)sampler.WrapW).U8((byte)sampler.CompareFunc)
            .U8((byte)sampler.Border).U8((byte)sampler.Anisotropy).U16(sampler.FilterFlags)
            .F32(sampler.MinLod).F32(sampler.MaxLod).F32(sampler.LodBias).Zeros(12);
    }
}
