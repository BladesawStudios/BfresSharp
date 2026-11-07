using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Writes materials in the Switch layout of version 10; <see cref="MaterialV10Codec"/> reads them.</summary>
/// <remarks>
/// The header is written with the other material headers. The body follows the render info tables, the zeroed texture
/// table, the samplers and their zeroed table, the parameter block and lookup table, the volatile flags, user data
/// array, slot arrays and texture names, then the shader info, the sampler dictionary, and, for the first material
/// of each shared assignment, the lists and dictionaries of that assignment.
/// </remarks>
public static class MaterialV10Writer
{
    private const int SamplerRuntimeLongs = 15;

    public readonly record struct Slots(
        int Info, int TextureTable, int TextureNames, int SamplerTable, int Samplers, int SamplerDict, int RenderInfoData, int RenderInfoCounts,
        int RenderInfoOffsets, int ParamData, int ParamIndices, int UserArray, int UserDict, int Volatile, int SamplerSlots, int TextureSlots);

    public static Slots WriteHeader(SwitchWriter writer, Material material, int index)
    {
        writer.Out.Bytes("FMAT"u8).U32(material.Flags);
        writer.StringPointer(material.Name);
        bool textured = material.TextureRefs.Count > 0;
        int Textured() => textured ? writer.Pointer() : writer.Unlisted();
        int info = writer.Pointer(), textureTable = Textured(), textureNames = Textured();
        int samplerTable = writer.Pointer(), samplers = writer.Pointer(), samplerDict = writer.Pointer();
        int infoData = writer.Pointer(), infoCounts = writer.Pointer(), infoOffsets = writer.Pointer();
        int paramData = writer.Pointer(), paramIndices = writer.Pointer();
        writer.Unlisted();
        int userArray = writer.Pointer(), userDict = writer.Pointer(), volatileFlags = writer.Pointer();
        writer.Unlisted();
        var slots = new Slots(info, textureTable, textureNames, samplerTable, samplers, samplerDict, infoData, infoCounts, infoOffsets, paramData, paramIndices, userArray, userDict, volatileFlags, Textured(), Textured());
        writer.Out.U16((ushort)index).U8((byte)material.TextureRefs.Count).U8((byte)material.Samplers.Count).U16(0)
            .U16((ushort)material.UserData.Count).U16(InfoDataSize(material)).U16(0).U16(0).U16(0);
        return slots;
    }

    // Writes the body up to the sampler dictionary and returns the slots of the user data values.
    public static int[] WriteBody(SwitchWriter writer, Material material, Slots slots, ShaderAssignSet assigns)
    {
        WriteRenderInfos(writer, material, slots);
        if (material.TextureRefs.Count > 0) WriteZeros(writer, slots.TextureTable, material.TextureRefs.Count * 8);
        if (material.Samplers.Count > 0)
        {
            writer.Align(8).Resolve(slots.Samplers);
            foreach (Sampler sampler in material.Samplers.Values) SamplerCodec.Write(writer, sampler);
            WriteZeros(writer, slots.SamplerTable, material.Samplers.Count * SamplerRuntimeLongs * 8);
        }

        if (material.ShaderParamData.Length > 0)
        {
            writer.Align(8).Resolve(slots.ParamData);
            writer.Out.Bytes(material.ShaderParamData);
        }

        if (material.ParamIndices.Length > 0)
        {
            writer.Align(8).Resolve(slots.ParamIndices);
            foreach (int value in material.ParamIndices) writer.Out.U32((uint)value);
        }

        if (material.VolatileFlags.Length > 0)
        {
            writer.Align(8).Resolve(slots.Volatile);
            writer.Out.Bytes(material.VolatileFlags);
        }

        int[] userSlots = UserDataCodec.WriteArray(writer, material.UserData, slots.UserArray);
        WriteSlots(writer, material.SamplerSlots, slots.SamplerSlots);
        WriteSlots(writer, material.TextureSlots, slots.TextureSlots);
        WriteTextureNames(writer, material, slots);
        writer.Align(8).Resolve(slots.Info);
        ShaderInfoCodec.Write(writer, material, assigns.PositionOf(material));
        if (material.Samplers.Count > 0) SwitchDictCodec.WriteAt(writer, slots.SamplerDict, material.Samplers);

        return userSlots;
    }

    public static void WriteUserData(SwitchWriter writer, Material material, Slots slots, int[] userSlots)
    {
        UserDataCodec.WriteDictAndValues(writer, material.UserData, slots.UserDict, userSlots);
    }

    private static void WriteRenderInfos(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.RenderInfos.Count == 0) return;
        RenderInfoCodec.PlaceValues(material.RenderInfos.Values);
        writer.Align(8).Resolve(slots.RenderInfoData);
        RenderInfoCodec.WriteBlock(writer, material.RenderInfos.Values, InfoDataSize(material));
        writer.Align(8).Resolve(slots.RenderInfoCounts);
        foreach (RenderInfo info in material.RenderInfos.Values) writer.Out.U16((ushort)info.Count);
        writer.Align(8).Resolve(slots.RenderInfoOffsets);
        foreach (RenderInfo info in material.RenderInfos.Values) writer.Out.U16((ushort)info.DataOffset);
    }

    // The block holds every value; a file may reserve more than the values need.
    private static ushort InfoDataSize(Material material)
    {
        int needed = material.RenderInfos.Values.Sum(RenderInfoCodec.ValueLength);
        return (ushort)Math.Max(needed, material.RenderInfoSize);
    }

    private static void WriteTextureNames(SwitchWriter writer, Material material, Slots slots)
    {
        if (material.TextureRefs.Count == 0) return;
        writer.Align(8).Resolve(slots.TextureNames);
        foreach (string name in material.TextureRefs) writer.StringPointer(name);
    }

    private static void WriteZeros(SwitchWriter writer, int slot, int length)
    {
        writer.Align(8).Resolve(slot);
        writer.Out.Zeros(length);
    }

    private static void WriteSlots(SwitchWriter writer, long[] values, int slot)
    {
        if (values.Length == 0) return;
        writer.Align(8).Resolve(slot);
        foreach (long value in values) writer.Out.U64((ulong)value);
    }
}
