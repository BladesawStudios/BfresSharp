using BfresSharp.Dictionaries;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes materials in the Switch layout of version 10.</summary>
/// <remarks>
/// After the magic and flags come the name, the shader info, the zeroed texture table, the texture names, the zeroed
/// sampler table, the sampler array and dictionary, then the render info data, count and offset tables, the parameter
/// block and its lookup table, a reserved word, the user data array and dictionary, the volatile flags, an unused
/// word and the sampler and texture slot arrays. The counts come last. Render infos and parameters are listed in the
/// shared shader assignment; the material holds their values.
/// </remarks>
public static class MaterialV10Codec
{
    public const int Size = 0xB0;
    private const int RenderInfoEntry = 16;
    private const int ParamEntry = 24;

    public static Material Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        long fields = at + 8, counts = fields + 0x98;
        int textures = view.U8(counts + 2), samplers = view.U8(counts + 3);
        var material = new Material
        {
            Name = reader.String(fields) ?? "",
            Flags = view.U32(at + 4),
            RenderInfoSize = view.U16(counts + 8),
            Samplers = reader.ReadDict(fields + 0x30, fields + 0x28, SamplerCodec.Size, offset => SamplerCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, fields + 0x70, fields + 0x68),
        };
        long info = reader.Pointer(fields + 8);
        ShaderAssignV10Codec.Tables tables = ShaderInfoCodec.Read(reader, info, material.ShaderAssign);
        ReadRenderInfos(reader, material, tables, fields);
        ReadParams(reader, material, tables, fields);
        long names = reader.Pointer(fields + 0x18);
        if (names != 0) material.TextureRefs = reader.ReadArray(names, textures, 8, offset => reader.String(offset) ?? "").ToList();
        long volatileFlags = reader.Pointer(fields + 0x78);
        if (volatileFlags != 0) material.VolatileFlags = view.Slice(volatileFlags, (tables.ParamCount + 7) / 8).Span.ToArray();
        long samplerSlots = reader.Pointer(fields + 0x88), textureSlots = reader.Pointer(fields + 0x90);
        if (samplerSlots != 0) material.SamplerSlots = reader.ReadArray(samplerSlots, samplers, 8, offset => (long)view.U64(offset));
        if (textureSlots != 0) material.TextureSlots = reader.ReadArray(textureSlots, textures, 8, offset => (long)view.U64(offset));
        return material;
    }

    private static void ReadRenderInfos(SwitchReader reader, Material material, ShaderAssignV10Codec.Tables tables, long fields)
    {
        var view = reader.View;
        long data = reader.Pointer(fields + 0x38), countTable = reader.Pointer(fields + 0x40), offsetTable = reader.Pointer(fields + 0x48);
        for (int index = 0; index < tables.RenderInfoCount; index++)
        {
            long entry = tables.RenderInfoList + index * (long)RenderInfoEntry;
            (string name, ulong? hash) = NameOf(reader, entry);
            int offset = view.U16(offsetTable + index * 2L);
            var info = new RenderInfo { Name = name, NameHash = hash ?? 0, DataOffset = offset };
            int count = view.U16(countTable + index * 2L);
            long values = data + offset;
            RenderInfoCodec.ReadValues(reader, info, (RenderInfoType)view.U8(entry + 8), values, count);
            material.RenderInfos.Add(name, info, hash);
        }

        if (material.RenderInfos.HasHashedKeys) material.RenderInfos.Layout = tables.RenderInfoNodes;
    }

    private static void ReadParams(SwitchReader reader, Material material, ShaderAssignV10Codec.Tables tables, long fields)
    {
        var view = reader.View;
        for (int index = 0; index < tables.ParamCount; index++)
        {
            long entry = tables.ParamList + index * (long)ParamEntry;
            (string name, ulong? hash) = NameOf(reader, entry + 8);
            var param = new ShaderParam { Name = name, NameHash = hash ?? 0, DataOffset = view.U16(entry + 0x10), Type = (ShaderParamType)view.U16(entry + 0x12) };
            material.ShaderParams.Add(name, param, hash);
        }

        if (material.ShaderParams.HasHashedKeys) material.ShaderParams.Layout = tables.ParamNodes;
        long block = reader.Pointer(fields + 0x50), lookup = reader.Pointer(fields + 0x58);
        if (block != 0) material.ShaderParamData = view.Slice(block, tables.ParamSize).Span.ToArray();
        if (lookup != 0) material.ParamIndices = reader.ReadArray(lookup, tables.ParamCount, 4, offset => view.I32(offset));
    }

    // A name field holds either a pointer to the name or the hash of a name the file leaves out.
    private static (string Name, ulong? Hash) NameOf(SwitchReader reader, long at)
    {
        (long target, ulong? hash) = reader.PointerOrHash(at);
        return hash is { } value ? ($"0x{value:X16}", value) : (target == 0 ? "" : reader.StringAtTarget(target), null);
    }
}
