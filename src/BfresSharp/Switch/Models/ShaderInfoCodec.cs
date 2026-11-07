using BfresSharp.Dictionaries;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads the shader info of a version 10 Switch material: its shader assignment and the choices made in it.</summary>
/// <remarks>
/// An info is 80 bytes. After the pointer to the shared assignment come, for the attribute assignments, the sampler
/// assignments and the options, an array of chosen names and an index array that says which name each dictionary key
/// takes (missing when every key takes the name at its own position, minus one for the shader's default). Options
/// that are switches are stored as bits of 64-bit words and the other choices as names after them.
/// </remarks>
public static class ShaderInfoCodec
{
    public const int Size = 0x50;

    public static ShaderAssignV10Codec.Tables Read(SwitchReader reader, long at, ShaderAssign assign)
    {
        var view = reader.View;
        ShaderAssignV10Codec.Tables tables = ShaderAssignV10Codec.Read(reader, reader.Pointer(at), assign);
        int attribs = view.U8(at + 0x44), samplers = view.U8(at + 0x45);
        int switches = view.U16(at + 0x46), choices = view.U16(at + 0x48);
        (assign.AttribAssigns, assign.AttribLayout) = Assigned(reader, tables.AttribNodes, Strings(reader, reader.Pointer(at + 8), attribs), reader.Pointer(at + 0x10), attribs);
        (assign.SamplerAssigns, assign.SamplerLayout) = Assigned(reader, tables.SamplerNodes, Strings(reader, reader.Pointer(at + 0x18), samplers), reader.Pointer(at + 0x20), samplers);
        (assign.ShaderOptions, assign.OptionLayout) = Options(reader, tables.OptionNodes, at, switches, choices);
        return tables;
    }

    // Writes the info and the arrays it points to; the shared assignment record is already placed at assignAt.
    public static void Write(SwitchWriter writer, Material material, long assignAt)
    {
        ShaderAssign assign = material.ShaderAssign;
        var attribs = Choices.Of(assign.AttribAssigns.Values, assign.AttribLayout);
        var samplers = Choices.Of(assign.SamplerAssigns.Values, assign.SamplerLayout);
        var options = OptionChoices.Of(assign.ShaderOptions.Values, assign.OptionLayout);
        writer.Pointer(assignAt);
        int Slot(bool present) => present ? writer.Pointer() : writer.Unlisted();
        int attribNames = writer.Pointer(), attribIndices = Slot(attribs.NeedsIndices);
        // An info whose shader has no sampler or option keys lists the same null pointers as one with options.
        bool hasOptions = options.Switches.Count + options.Values.Count > 0 || assign.SamplerAssigns.Count + assign.ShaderOptions.Count == 0;
        int samplerNames = Slot(samplers.Names.Count > 0 || hasOptions), samplerIndices = Slot(samplers.NeedsIndices);
        int bits = Slot(hasOptions), values = Slot(hasOptions), optionIndices = Slot(options.NeedsIndices);
        writer.Out.U32(0).U8((byte)attribs.Names.Count).U8((byte)samplers.Names.Count).U16((ushort)options.Switches.Count)
            .U16((ushort)(options.Switches.Count + options.Values.Count)).U16(0).U32(0);
        WriteNames(writer, attribNames, attribs.Names);
        WriteIndices(writer, attribIndices, attribs);
        WriteNames(writer, samplerNames, samplers.Names);
        WriteIndices(writer, samplerIndices, samplers);
        if (options.Switches.Count > 0)
        {
            writer.Align(8).Resolve(bits);
            for (int word = 0; word <= options.Switches.Count / 64; word++) writer.Out.U64(options.Word(word));
        }

        WriteNames(writer, values, options.Values);
        if (!options.NeedsIndices) return;
        writer.Align(8).Resolve(optionIndices);
        foreach (int key in options.KeysByName()) writer.Out.U16((ushort)key);
        foreach (int slot in options.Slots) writer.Out.U16((ushort)slot);
        writer.Align(8);
    }

    private static void WriteNames(SwitchWriter writer, int slot, IReadOnlyList<string> names)
    {
        if (names.Count == 0) return;
        writer.Align(8).Resolve(slot);
        foreach (string name in names) writer.StringPointer(name);
    }

    private static void WriteIndices(SwitchWriter writer, int slot, Choices choices)
    {
        if (!choices.NeedsIndices) return;
        writer.Align(8).Resolve(slot);
        foreach (int key in choices.KeysByName()) writer.Out.U8((byte)key);
        foreach (int value in choices.Slots) writer.Out.U8((byte)value);
        writer.Align(8);
    }

    private static (ResDict<string>, ChoiceLayout) Assigned(SwitchReader reader, IReadOnlyList<DictNode> nodes, string[] names, long indices, int used)
    {
        var result = new ResDict<string>();
        var slots = new List<int>();
        for (int index = 1; index < nodes.Count; index++)
        {
            int slot = indices == 0 ? index - 1 : (sbyte)reader.View.U8(indices + used + index - 1);
            slots.Add(slot);
            result.Add(SwitchReader.KeyOf(nodes[index]), slot < 0 ? ShaderAssign.DefaultValue : names[slot], nodes[index].Hash);
        }

        if (result.HasHashedKeys) result.Layout = nodes;
        return (result, new ChoiceLayout(names, slots));
    }

    private static (ResDict<string>, ChoiceLayout) Options(SwitchReader reader, IReadOnlyList<DictNode> nodes, long at, int switches, int choices)
    {
        var view = reader.View;
        long bits = reader.Pointer(at + 0x28), indices = reader.Pointer(at + 0x38);
        var names = new List<string>();
        for (int bit = 0; bit < switches; bit++) names.Add((view.U64(bits + bit / 64 * 8) >> (bit % 64) & 1) != 0 ? "True" : "False");
        names.AddRange(Strings(reader, reader.Pointer(at + 0x30), choices - switches));
        var result = new ResDict<string>();
        var slots = new List<int>();
        for (int index = 1; index < nodes.Count; index++)
        {
            int slot = indices == 0 ? index - 1 : (short)view.U16(indices + (choices + index - 1) * 2L);
            slots.Add(slot);
            result.Add(SwitchReader.KeyOf(nodes[index]), slot < 0 ? ShaderAssign.DefaultValue : names[slot], nodes[index].Hash);
        }

        if (result.HasHashedKeys) result.Layout = nodes;
        return (result, new ChoiceLayout(names, slots, switches));
    }

    private static string[] Strings(SwitchReader reader, long at, int count)
    {
        return at == 0 ? [] : reader.ReadArray(at, count, 8, offset => reader.String(offset) ?? "");
    }
}
