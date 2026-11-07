using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes the shader assignment of a version 5 Switch material.</summary>
/// <remarks>
/// The archive and shading model names, the pointer pairs of the attribute, sampler and option assignments, the
/// revision and the three counts. Each assignment dictionary holds name pointers as its values.
/// </remarks>
public static class ShaderAssignCodec
{
    public const int Size = 0x48;

    public static ShaderAssign Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        return new ShaderAssign
        {
            ShaderArchiveName = reader.String(at) ?? "",
            ShadingModelName = reader.String(at + 8) ?? "",
            AttribAssigns = ReadStrings(reader, at + 0x18, at + 0x10),
            SamplerAssigns = ReadStrings(reader, at + 0x28, at + 0x20),
            ShaderOptions = ReadStrings(reader, at + 0x38, at + 0x30),
            Revision = view.U32(at + 0x40),
        };
    }

    public static Slots WriteHeader(SwitchWriter writer, ShaderAssign assign)
    {
        writer.StringPointer(assign.ShaderArchiveName);
        writer.StringPointer(assign.ShadingModelName);
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        writer.Out.U32(assign.Revision).U8((byte)assign.AttribAssigns.Count).U8((byte)assign.SamplerAssigns.Count)
            .U16((ushort)assign.ShaderOptions.Count);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, ShaderAssign assign, Slots slots)
    {
        WriteStrings(writer, assign.AttribAssigns, slots.Attribs, slots.AttribDict);
        WriteStrings(writer, assign.SamplerAssigns, slots.Samplers, slots.SamplerDict);
        WriteStrings(writer, assign.ShaderOptions, slots.Options, slots.OptionDict);
    }

    private static ResDict<string> ReadStrings(SwitchReader reader, long dictAt, long arrayAt)
    {
        return reader.ReadDict(dictAt, arrayAt, 8, offset => reader.String(offset) ?? "");
    }

    private static void WriteStrings(SwitchWriter writer, ResDict<string> values, int arraySlot, int dictSlot)
    {
        if (values.Count == 0) return;
        writer.Align(8).Resolve(arraySlot);
        foreach (string value in values.Values) writer.StringPointer(value);
        writer.Align(8).Resolve(dictSlot);
        SwitchDictCodec.Write(writer, values);
    }

    public readonly record struct Slots(int Attribs, int AttribDict, int Samplers, int SamplerDict, int Options, int OptionDict);
}
