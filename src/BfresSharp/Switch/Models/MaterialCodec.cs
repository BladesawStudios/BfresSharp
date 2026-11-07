using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes materials in the Switch layout of versions before 10.</summary>
/// <remarks>
/// After the magic come the name, the render info, shader assignment, texture, sampler, parameter and user data
/// pointers, the volatile flag and slot pointers, then the counts. Files before version 9 hold the flags before the
/// counts and four unused bytes after them. The slot arrays are 8 bytes a texture and a sampler.
/// </remarks>
public static class MaterialCodec
{
    private const int SamplerRuntimeLongs = 15;

    public static int Size(SwitchProfile profile) => profile.Modern ? 0xA8 : 0xB8;

    public static Material Read(SwitchReader reader, long at)
    {
        // TODO: Version 10 stores the shader assignment, render infos and parameters in a different layout.
        if (reader.Profile.Version.Major >= 10) throw new BfresUnsupportedException("Reading version 10 materials is not supported yet", at);
        var view = reader.View;
        var profile = reader.Profile;
        long fields = at + (profile.Modern ? 8 : 0x10);
        long counts = fields + 0x90 + (profile.Modern ? 0 : 4);
        int textures = view.U8(counts + 4);
        int samplers = view.U8(counts + 5);
        int paramCount = view.U16(counts + 6);
        var material = new Material
        {
            Name = reader.String(fields) ?? "",
            Flags = view.U32(profile.Modern ? at + 4 : fields + 0x90),
            RenderInfos = reader.ReadDict(fields + 0x10, fields + 8, RenderInfoCodec.Size, offset => RenderInfoCodec.Read(reader, offset)),
            Samplers = reader.ReadDict(fields + 0x40, fields + 0x38, SamplerCodec.Size, offset => SamplerCodec.Read(reader, offset)),
            ShaderParams = reader.ReadDict(fields + 0x50, fields + 0x48, ShaderParamCodec.Size, offset => ShaderParamCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, fields + 0x68, fields + 0x60),
        };
        long assign = reader.Pointer(fields + 0x18);
        if (assign != 0) material.ShaderAssign = ShaderAssignCodec.Read(reader, assign);
        long names = reader.Pointer(fields + 0x28);
        if (names != 0) material.TextureRefs = reader.ReadArray(names, textures, 8, offset => reader.String(offset) ?? "").ToList();
        long block = reader.Pointer(fields + 0x58);
        if (block != 0) material.ShaderParamData = view.Slice(block, view.U16(counts + 0xA)).Span.ToArray();
        long volatileFlags = reader.Pointer(fields + 0x70);
        if (volatileFlags != 0) material.VolatileFlags = view.Slice(volatileFlags, (paramCount + 7) / 8).Span.ToArray();
        long samplerSlots = reader.Pointer(fields + 0x80);
        if (samplerSlots != 0) material.SamplerSlots = reader.ReadArray(samplerSlots, samplers, 8, offset => (long)view.U64(offset));
        long textureSlots = reader.Pointer(fields + 0x88);
        if (textureSlots != 0) material.TextureSlots = reader.ReadArray(textureSlots, textures, 8, offset => (long)view.U64(offset));
        return material;
    }

    public static Slots WriteHeader(SwitchWriter writer, Material material, int index)
    {
        var profile = writer.Profile;
        writer.Out.Bytes("FMAT"u8);
        if (profile.Modern) writer.Out.U32(material.Flags);
        else writer.Block();
        writer.StringPointer(material.Name);
        // Pointers that belong to textures are not recorded for a material without any.
        bool textured = material.TextureRefs.Count > 0;
        int Textured() => textured ? writer.Pointer() : writer.Unlisted();
        var slots = new Slots
        {
            RenderInfos = writer.Pointer(), RenderInfoDict = writer.Pointer(), ShaderAssign = writer.Pointer(),
            TextureUnknown = Textured(), TextureNames = Textured(), SamplerUnknown = writer.Pointer(),
            Samplers = writer.Pointer(), SamplerDict = writer.Pointer(), Params = writer.Pointer(), ParamDict = writer.Pointer(),
            ParamData = writer.Pointer(), UserArray = writer.Pointer(), UserDict = writer.Pointer(), Volatile = writer.Pointer(),
        };
        writer.Unlisted();
        slots = slots with { SamplerSlots = Textured(), TextureSlots = Textured() };
        if (!profile.Modern) writer.Out.U32(material.Flags);
        writer.Out.U16((ushort)index).U16((ushort)material.RenderInfos.Count).U8((byte)material.TextureRefs.Count)
            .U8((byte)material.Samplers.Count).U16((ushort)material.ShaderParams.Count).U16(0)
            .U16((ushort)material.ShaderParamData.Length).U16(0).U16((ushort)material.UserData.Count);
        if (!profile.Modern) writer.Out.U32(0);
        return slots;
    }

    // The render infos and their values, the zeroed runtime tables, samplers, parameters, slots and names, the shader
    // assignment, then the dictionaries and user data values.
    public static void WriteBody(SwitchWriter writer, Material material, Slots slots)
    {
        var valueSlots = WriteRenderInfos(writer, material, slots);
        if (valueSlots.Count > 0) RenderInfoCodec.WriteValues(writer, material.RenderInfos.Values, valueSlots);
        WriteZeros(writer, slots.TextureUnknown, material.TextureRefs.Count * 8);
        WriteSamplers(writer, material, slots);
        WriteParams(writer, material, slots);
        if (material.VolatileFlags.Length > 0)
        {
            writer.Align(8).Resolve(slots.Volatile);
            writer.Out.Bytes(material.VolatileFlags);
        }

        int[] userSlots = UserDataCodec.WriteArray(writer, material.UserData, slots.UserArray);
        WriteSlots(writer, material.SamplerSlots, slots.SamplerSlots);
        WriteSlots(writer, material.TextureSlots, slots.TextureSlots);
        WriteTextures(writer, material, slots);
        writer.Align(8).Resolve(slots.ShaderAssign);
        var assignSlots = ShaderAssignCodec.WriteHeader(writer, material.ShaderAssign);
        ShaderAssignCodec.WriteBody(writer, material.ShaderAssign, assignSlots);
        WriteDictionary(writer, slots.RenderInfoDict, material.RenderInfos);
        WriteDictionary(writer, slots.SamplerDict, material.Samplers);
        WriteDictionary(writer, slots.ParamDict, material.ShaderParams);
        UserDataCodec.WriteDictAndValues(writer, material.UserData, slots.UserDict, userSlots);
    }

    private static List<int> WriteRenderInfos(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.RenderInfos.Count == 0) return [];
        writer.Align(8).Resolve(slots.RenderInfos);
        return material.RenderInfos.Values.Select(info => RenderInfoCodec.WriteRecord(writer, info)).ToList();
    }

    private static void WriteDictionary<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        writer.Align(8).Resolve(slot);
        SwitchDictCodec.Write(writer, dict);
    }

    private static void WriteZeros(SwitchWriter writer, int slot, int length)
    {
        if (length == 0) return;
        writer.Align(8).Resolve(slot);
        writer.Out.Zeros(length);
    }

    private static void WriteTextures(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.TextureRefs.Count == 0) return;
        writer.Align(8).Resolve(slots.TextureNames);
        foreach (string name in material.TextureRefs) writer.StringPointer(name);
    }

    private static void WriteSamplers(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.Samplers.Count == 0)
        {
            // Without samplers the pointers still land where the parameters start.
            writer.Align(8).Resolve(slots.Samplers);
            writer.Resolve(slots.SamplerUnknown);
            return;
        }

        writer.Align(8).Resolve(slots.Samplers);
        foreach (Sampler sampler in material.Samplers.Values) SamplerCodec.Write(writer, sampler);
        WriteZeros(writer, slots.SamplerUnknown, material.Samplers.Count * SamplerRuntimeLongs * 8);
    }

    private static void WriteParams(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.ShaderParams.Count > 0)
        {
            writer.Align(8).Resolve(slots.Params);
            foreach (ShaderParam param in material.ShaderParams.Values) ShaderParamCodec.Write(writer, param);
        }

        if (material.ShaderParamData.Length == 0) return;
        writer.Align(8).Resolve(slots.ParamData);
        writer.Out.Bytes(material.ShaderParamData);
    }

    private static void WriteSlots(SwitchWriter writer, long[] values, int slot)
    {
        if (values.Length == 0) return;
        writer.Align(8).Resolve(slot);
        foreach (long value in values) writer.Out.U64((ulong)value);
    }

    public readonly record struct Slots
    {
        public int RenderInfos { get; init; }
        public int RenderInfoDict { get; init; }
        public int ShaderAssign { get; init; }
        public int TextureUnknown { get; init; }
        public int TextureNames { get; init; }
        public int SamplerUnknown { get; init; }
        public int Samplers { get; init; }
        public int SamplerDict { get; init; }
        public int Params { get; init; }
        public int ParamDict { get; init; }
        public int ParamData { get; init; }
        public int UserArray { get; init; }
        public int UserDict { get; init; }
        public int Volatile { get; init; }
        public int SamplerSlots { get; init; }
        public int TextureSlots { get; init; }
    }
}
