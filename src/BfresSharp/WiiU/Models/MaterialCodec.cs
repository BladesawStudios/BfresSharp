using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;
using BfresSharp.WiiU.Texture;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes materials in the Wii U layout, which is 76 bytes from version 3.3.</summary>
/// <remarks>
/// The magic, name, flags, index and the counts of render infos, samplers, texture refs, parameters, volatile parameters,
/// parameter data bytes and user data, then offsets to the render info dictionary, render state, shader assignment, texture
/// ref list, sampler list and dictionary, parameter list and dictionary, parameter data, user data dictionary and
/// volatile flags. A texture ref is a name and an offset to the texture, which is zero when the texture is in another file.
/// </remarks>
public static class MaterialCodec
{
    public const int Size = 0x4C;
    private const int TextureRefSize = 8;

    public static Material Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        int refCount = view.U8(at + 0x11), paramCount = view.U16(at + 0x12);
        var material = new Material
        {
            Name = reader.String(at + 4) ?? "",
            Flags = view.U32(at + 8),
            RenderInfos = reader.ReadDict(at + 0x1C, offset => RenderInfoCodec.Read(reader, offset)),
            RenderState = reader.ReadOne(at + 0x20, offset => RenderStateCodec.Read(reader, offset)),
            Samplers = ReadSamplers(reader, at),
            ShaderParams = reader.ReadDict(at + 0x38, offset => ShaderCodec.ReadParam(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, at + 0x40),
            TextureRefs = reader.ReadList(at + 0x28, refCount, TextureRefSize, offset => reader.String(offset) ?? ""),
        };
        material.ShaderAssign = reader.ReadOne(at + 0x24, offset => ShaderCodec.ReadAssign(reader, offset)) ?? material.ShaderAssign;
        long data = reader.Offset(at + 0x3C), flags = reader.Offset(at + 0x44);
        if (data != 0) material.ShaderParamData = view.Slice(data, view.U16(at + 0x16)).Span.ToArray();
        if (flags != 0) material.VolatileFlags = view.Slice(flags, (paramCount + 7) / 8).Span.ToArray();
        return material;
    }

    public static void Write(WiiUWriter writer, Material material, ResDict<Model.Texture.Texture> textures, int index)
    {
        var o = writer.Out;
        o.Bytes("FMAT"u8);
        writer.Name(material.Name);
        o.U32(material.Flags).U16((ushort)index).U16((ushort)material.RenderInfos.Count).U8((byte)material.Samplers.Count)
            .U8((byte)material.TextureRefs.Count).U16((ushort)material.ShaderParams.Count).U16(0)
            .U16((ushort)material.ShaderParamData.Length).U16(0).U16((ushort)material.UserData.Count);
        writer.Dict(material.RenderInfos, (info, _) => RenderInfoCodec.Write(writer, info));
        writer.Item(material.RenderState, -1, (state, _) => RenderStateCodec.Write(writer, state));
        writer.Item(material.ShaderAssign, -1, (assign, _) => ShaderCodec.WriteAssign(writer, assign));
        var refs = material.TextureRefs.Select(name => new TextureRef(name)).ToList();
        writer.List(refs, (reference, _) => WriteTextureRef(writer, reference, textures), pointWhenEmpty: true);
        writer.List(material.Samplers.Values, (sampler, at) => ShaderCodec.WriteSampler(writer, sampler, material.Samplers.Keys[at], at), pointWhenEmpty: true);
        writer.Dict(material.Samplers, (sampler, at) => ShaderCodec.WriteSampler(writer, sampler, material.Samplers.Keys[at], at));
        writer.List(material.ShaderParams.Values, (param, _) => ShaderCodec.WriteParam(writer, param));
        writer.Dict(material.ShaderParams, (param, _) => ShaderCodec.WriteParam(writer, param));
        writer.Custom(material.ShaderParamData.Length == 0 ? null : material.ShaderParamData, () => o.Bytes(material.ShaderParamData));
        UserDataCodec.WriteDict(writer, material.UserData);
        object volatileFlags = writer.Array(() => o.Bytes(material.VolatileFlags));
        o.U32(0);
        writer.PlaceValues(material.RenderInfos);
        writer.Place(material.RenderState, refs, material.Samplers.Values, material.ShaderParams.Values, material.ShaderParamData, volatileFlags, material.ShaderAssign);
        writer.Place(material.RenderInfos, material.Samplers, material.ShaderParams);
        writer.PlaceAll(material.UserData);
    }

    private static ResDict<Sampler> ReadSamplers(WiiUReader reader, long at)
    {
        var samplers = reader.ReadDict(at + 0x30, offset => ShaderCodec.ReadSampler(reader, offset));
        return samplers;
    }

    private static void WriteTextureRef(WiiUWriter writer, TextureRef reference, ResDict<Model.Texture.Texture> textures)
    {
        writer.Name(reference.Name);
        writer.Item(textures.TryGetValue(reference.Name, out var texture) ? texture : null, -1, (texture, _) => TextureCodec.Write(writer, texture));
    }

    private sealed record TextureRef(string Name);
}
