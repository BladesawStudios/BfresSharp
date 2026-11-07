using BfresSharp.Dictionaries;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes the shared shader assignment records of version 10 Switch models.</summary>
/// <remarks>
/// A record is 88 bytes: the archive and shading model names, then a pointer to a list and a pointer to a dictionary
/// for the render infos and the shader parameters, dictionaries for the attribute and sampler assignments and the
/// options, the render info and parameter counts, the size of the parameter block and padding. The lists give each
/// entry's name (or hash), type and offset; the values live in the materials that share the record.
/// </remarks>
public static class ShaderAssignV10Codec
{
    public const int Size = 0x58;

    /// <summary>Where a record's lists are and the dictionaries it keeps.</summary>
    public sealed record Tables(
        long RenderInfoList,
        long ParamList,
        int RenderInfoCount,
        int ParamCount,
        int ParamSize,
        IReadOnlyList<DictNode> RenderInfoNodes,
        IReadOnlyList<DictNode> ParamNodes,
        IReadOnlyList<DictNode> AttribNodes,
        IReadOnlyList<DictNode> SamplerNodes,
        IReadOnlyList<DictNode> OptionNodes);

    public static Tables Read(SwitchReader reader, long at, ShaderAssign assign)
    {
        var view = reader.View;
        assign.ShaderArchiveName = reader.String(at) ?? "";
        assign.ShadingModelName = reader.String(at + 8) ?? "";
        return new Tables(
            reader.Pointer(at + 0x10),
            reader.Pointer(at + 0x20),
            view.U16(at + 0x48),
            view.U16(at + 0x4A),
            view.U16(at + 0x4C),
            Nodes(reader, at + 0x18),
            Nodes(reader, at + 0x28),
            Nodes(reader, at + 0x30),
            Nodes(reader, at + 0x38),
            Nodes(reader, at + 0x40));
    }

    /// <summary>The slots of the pointers a record leaves to be resolved.</summary>
    public readonly record struct Slots(int RenderInfos, int RenderInfoDict, int Params, int ParamDict, int Attribs, int Samplers, int Options);

    public static Slots WriteRecord(SwitchWriter writer, Material material)
    {
        ShaderAssign assign = material.ShaderAssign;
        writer.StringPointer(assign.ShaderArchiveName);
        writer.StringPointer(assign.ShadingModelName);
        int Dict(int count) => writer.Pointer();
        var slots = new Slots(
            Dict(material.RenderInfos.Count), Dict(material.RenderInfos.Count), Dict(material.ShaderParams.Count), Dict(material.ShaderParams.Count),
            Dict(assign.AttribAssigns.Count), Dict(assign.SamplerAssigns.Count), Dict(assign.ShaderOptions.Count));
        writer.Out.U16((ushort)material.RenderInfos.Count).U16((ushort)material.ShaderParams.Count).U16((ushort)material.ShaderParamData.Length)
            .U16(0).U64(0);
        return slots;
    }

    // The render info and parameter lists, then the dictionaries of the parameters, render infos, attributes, samplers and options.
    public static void WriteBody(SwitchWriter writer, Material material, Slots slots)
    {
        ShaderAssign assign = material.ShaderAssign;
        if (material.RenderInfos.Count > 0)
        {
            writer.Align(8).Resolve(slots.RenderInfos);
            foreach (RenderInfo info in material.RenderInfos.Values) WriteRenderInfo(writer, info);
        }

        if (material.ShaderParams.Count > 0)
        {
            writer.Align(8).Resolve(slots.Params);
            foreach (ShaderParam param in material.ShaderParams.Values) WriteParam(writer, param);
        }

        WriteDict(writer, slots.ParamDict, material.ShaderParams);
        WriteDict(writer, slots.RenderInfoDict, material.RenderInfos);
        WriteDict(writer, slots.Attribs, assign.AttribAssigns);
        WriteDict(writer, slots.Samplers, assign.SamplerAssigns);
        WriteDict(writer, slots.Options, assign.ShaderOptions);
    }

    private static void WriteRenderInfo(SwitchWriter writer, RenderInfo info)
    {
        writer.NamePointer(info.Name, info.NameHash);
        writer.Out.U8((byte)info.Type).Zeros(7);
    }

    private static void WriteParam(SwitchWriter writer, ShaderParam param)
    {
        writer.Unlisted();
        writer.NamePointer(param.Name, param.NameHash);
        writer.Out.U16(param.DataOffset).U16((ushort)param.Type).U32(0);
    }

    private static void WriteDict<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        SwitchDictCodec.WriteAt(writer, slot, dict);
    }

    private static IReadOnlyList<DictNode> Nodes(SwitchReader reader, long pointerAt)
    {
        long dict = reader.Pointer(pointerAt);
        return dict == 0 ? [] : reader.Dictionary(dict);
    }
}
