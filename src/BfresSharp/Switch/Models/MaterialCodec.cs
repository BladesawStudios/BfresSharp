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
    public static int Size(SwitchProfile profile) => profile.Modern ? 0xA8 : 0xB8;

    public static Material Read(SwitchReader reader, long at)
    {
        // TODO: Version 10 stores the shader assignment, render infos and parameters in a different layout.
        if (reader.Profile.Version.Major >= 10) throw new BfresFormatException("Reading version 10 materials is not supported yet", at);
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
        var slots = new Slots
        {
            RenderInfos = writer.Pointer(), RenderInfoDict = writer.Pointer(), ShaderAssign = writer.Pointer(),
            TextureUnknown = writer.Pointer(), TextureNames = writer.Pointer(), SamplerUnknown = writer.Pointer(),
            Samplers = writer.Pointer(), SamplerDict = writer.Pointer(), Params = writer.Pointer(), ParamDict = writer.Pointer(),
            ParamData = writer.Pointer(), UserArray = writer.Pointer(), UserDict = writer.Pointer(), Volatile = writer.Pointer(),
        };
        writer.Unlisted();
        slots = slots with { SamplerSlots = writer.Pointer(), TextureSlots = writer.Pointer() };
        if (!profile.Modern) writer.Out.U32(material.Flags);
        writer.Out.U16((ushort)index).U16((ushort)material.RenderInfos.Count).U8((byte)material.TextureRefs.Count)
            .U8((byte)material.Samplers.Count).U16((ushort)material.ShaderParams.Count).U16(0)
            .U16((ushort)material.ShaderParamData.Length).U16(0).U16((ushort)material.UserData.Count);
        if (!profile.Modern) writer.Out.U32(0);
        return slots;
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
