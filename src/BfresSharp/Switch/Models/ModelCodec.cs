using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes models in the Switch layout.</summary>
/// <remarks>
/// After the magic come the name, the path, the skeleton, the vertex buffer array, the shape array and dictionary, the
/// material array and dictionary and the user data array and dictionary. Version 9 files pad the material pointers and
/// version 10 adds a shader assign array. The vertex buffer, shape, material and user data counts come last.
/// </remarks>
public static class ModelCodec
{
    public static int Size(SwitchProfile profile) => new ModelLayout(profile).Size;

    public static ResModel Read(SwitchReader reader, long at)
    {
        var layout = new ModelLayout(reader.Profile);
        var view = reader.View;
        long fields = at + layout.Fields;
        int buffers = view.U16(fields + layout.Counts);
        int materialSize = MaterialSize(reader.Profile);
        var model = new ResModel
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Flags = reader.Profile.Modern ? view.U32(at + 4) : 0,
            TotalVertexCount = reader.Profile.Modern ? 0 : view.U32(fields + layout.Counts + 8),
            Skeleton = SkeletonCodec.Read(reader, reader.Pointer(fields + ModelLayout.Skeleton)),
            Shapes = reader.ReadDict(fields + ModelLayout.ShapeDict, fields + ModelLayout.Shapes, ShapeCodec.Size(reader.Profile), offset => ShapeCodec.Read(reader, offset)),
            Materials = reader.ReadDict(fields + layout.MaterialDict, fields + ModelLayout.Materials, materialSize, offset => ReadMaterial(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, fields + layout.UserDict, fields + layout.UserArray),
        };
        long vertexBuffers = reader.Pointer(fields + ModelLayout.VertexBuffers);
        if (vertexBuffers != 0) model.VertexBuffers = reader.ReadArray(vertexBuffers, buffers, VertexBufferCodec.Size(reader.Profile), offset => VertexBufferCodec.Read(reader, offset)).ToList();
        return model;
    }

    private static int MaterialSize(SwitchProfile profile) => profile.Version.Major >= 10 ? MaterialV10Codec.Size : MaterialCodec.Size(profile);

    private static Material ReadMaterial(SwitchReader reader, long at)
    {
        return reader.Profile.Version.Major >= 10 ? MaterialV10Codec.Read(reader, at) : MaterialCodec.Read(reader, at);
    }

    public static Slots WriteHeader(SwitchWriter writer, ResModel model)
    {
        var profile = writer.Profile;
        var layout = new ModelLayout(profile);
        writer.Out.Bytes("FMDL"u8);
        if (profile.Modern) writer.Out.U32(model.Flags);
        else writer.Block();
        writer.StringPointer(model.Name);
        writer.StringPointer(model.Path);
        int skeleton = writer.Pointer(), buffers = writer.Pointer(), shapes = writer.Pointer(), shapeDict = writer.Pointer(), materials = writer.Pointer();
        if (profile.Version.Major == 9) writer.Unlisted();
        int materialDict = writer.Pointer();
        var section = new MaterialSection(profile, model);
        int assigns = layout.HasShaderAssigns ? writer.Pointer() : 0;
        var slots = new Slots(skeleton, buffers, shapes, shapeDict, materials, materialDict, writer.Pointer(), writer.Pointer(), assigns, section);
        writer.Unlisted();
        writer.Out.U16((ushort)model.VertexBuffers.Count).U16((ushort)model.Shapes.Count).U16((ushort)model.Materials.Count);
        if (profile.Modern) writer.Out.U16((ushort)section.Assigns.Count).U16((ushort)model.UserData.Count).U16(0).U32(0);
        else writer.Out.U16((ushort)model.UserData.Count).U32(model.TotalVertexCount).U32(0);
        int extension = writer.Position;
        if (layout.HasExtension) writer.Out.Zeros(ModelLayout.ExtensionSize);
        return slots with { ExtensionAt = extension };
    }

    // The vertex buffer, material and shape headers, the skeleton, the dictionaries and then what each section points to.
    public static void WriteBody(SwitchWriter writer, ResModel model, Slots slots, SwitchBufferLayout layout)
    {
        // With user data every array is pointed at, even an empty one.
        bool any = model.UserData.Count > 0;
        if (any || model.VertexBuffers.Count > 0) writer.Align(8).Resolve(slots.VertexBuffers);
        var bufferPositions = new List<int>();
        var bufferSlots = new List<VertexBufferCodec.Slots>();
        for (int index = 0; index < model.VertexBuffers.Count; index++)
        {
            bufferPositions.Add(writer.Position);
            bufferSlots.Add(VertexBufferCodec.WriteHeader(writer, model.VertexBuffers[index], index, layout.StreamOffset(model.VertexBuffers[index])));
        }

        if (any || model.Materials.Count > 0) writer.Align(8).Resolve(slots.Materials);
        slots.Section.WriteHeaders(writer);
        slots.Section.WriteAssignRecords(writer, slots.ShaderAssigns);
        if (any || model.Shapes.Count > 0) writer.Align(8).Resolve(slots.Shapes);
        var shapeSlots = model.Shapes.Values.Select((shape, index) => ShapeCodec.WriteHeader(writer, shape, bufferPositions[shape.VertexBufferIndex], index)).ToList();
        // An empty user data array is only pointed at when the model has geometry before it.
        bool geometry = model.Shapes.Count > 0 || model.Materials.Count > 0 || model.VertexBuffers.Count > 0;
        int[] userSlots = geometry || model.UserData.Count > 0 ? UserDataCodec.WriteArray(writer, model.UserData, slots.UserArray) : [];
        writer.Align(8).Resolve(slots.Skeleton);
        var skeletonSlots = SkeletonCodec.WriteHeader(writer, model.Skeleton);
        SkeletonCodec.WriteBody(writer, model.Skeleton, skeletonSlots);
        WriteDictionary(writer, slots.ShapeDict, model.Shapes);
        WriteDictionary(writer, slots.MaterialDict, model.Materials);
        if (!writer.Profile.Modern) UserDataCodec.WriteDict(writer, model.UserData, slots.UserDict);
        for (int index = 0; index < shapeSlots.Count; index++) ShapeCodec.WriteBody(writer, model.Shapes.Values[index], shapeSlots[index], layout);
        for (int index = 0; index < bufferSlots.Count; index++) VertexBufferCodec.WriteBody(writer, model.VertexBuffers[index], bufferSlots[index]);
        UserDataCodec.WriteValuesAligned(writer, model.UserData, userSlots);
        slots.Section.WriteBodies(writer);
        // Version 9 and later keep the user data dictionary after everything else.
        if (writer.Profile.Modern) UserDataCodec.WriteDict(writer, model.UserData, slots.UserDict);
    }

    private static void WriteDictionary<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        writer.Align(8).Resolve(slot);
        SwitchDictCodec.Write(writer, dict);
    }

    public readonly record struct Slots(
        int Skeleton, int VertexBuffers, int Shapes, int ShapeDict, int Materials, int MaterialDict, int UserArray, int UserDict, int ShaderAssigns,
        MaterialSection Section, int ExtensionAt = 0);
}
