using BfresSharp.Errors;
using BfresSharp.Model;
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
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x68 : 0x78;

    public static ResModel Read(SwitchReader reader, long at)
    {
        // TODO: Versions 9 and later have a different pointer layout and the shader assign array.
        RequireSupported(reader.Profile, at);
        var view = reader.View;
        long fields = at + 0x10;
        int buffers = view.U16(fields + 0x58);
        var model = new ResModel
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            TotalVertexCount = view.U32(fields + 0x60),
            Skeleton = SkeletonCodec.Read(reader, reader.Pointer(fields + 0x10)),
            Shapes = reader.ReadDict(fields + 0x28, fields + 0x20, ShapeCodec.Size(reader.Profile), offset => ShapeCodec.Read(reader, offset)),
            Materials = reader.ReadDict(fields + 0x38, fields + 0x30, MaterialCodec.Size(reader.Profile), offset => MaterialCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, fields + 0x48, fields + 0x40),
        };
        long vertexBuffers = reader.Pointer(fields + 0x18);
        if (vertexBuffers != 0) model.VertexBuffers = reader.ReadArray(vertexBuffers, buffers, VertexBufferCodec.Size(reader.Profile), offset => VertexBufferCodec.Read(reader, offset)).ToList();
        return model;
    }

    public static Slots WriteHeader(SwitchWriter writer, ResModel model)
    {
        RequireSupported(writer.Profile, writer.Position);
        writer.Out.Bytes("FMDL"u8);
        writer.Block();
        writer.StringPointer(model.Name);
        writer.StringPointer(model.Path);
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        writer.Unlisted();
        writer.Out.U16((ushort)model.VertexBuffers.Count).U16((ushort)model.Shapes.Count).U16((ushort)model.Materials.Count)
            .U16((ushort)model.UserData.Count).U32(model.TotalVertexCount).U32(0);
        return slots;
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
        var materialSlots = model.Materials.Values.Select((material, index) => MaterialCodec.WriteHeader(writer, material, index)).ToList();
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
        UserDataCodec.WriteDict(writer, model.UserData, slots.UserDict);
        for (int index = 0; index < shapeSlots.Count; index++) ShapeCodec.WriteBody(writer, model.Shapes.Values[index], shapeSlots[index], layout);
        for (int index = 0; index < bufferSlots.Count; index++) VertexBufferCodec.WriteBody(writer, model.VertexBuffers[index], bufferSlots[index]);
        UserDataCodec.WriteValuesAligned(writer, model.UserData, userSlots);
        for (int index = 0; index < materialSlots.Count; index++) MaterialCodec.WriteBody(writer, model.Materials.Values[index], materialSlots[index]);
    }

    private static void WriteDictionary<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        writer.Align(8).Resolve(slot);
        SwitchDictCodec.Write(writer, dict);
    }

    private static void RequireSupported(SwitchProfile profile, long at)
    {
        if (profile.Version.Major >= 9) throw new BfresFormatException("Models of version 9 and later are not supported yet", at);
    }

    public readonly record struct Slots(
        int Skeleton, int VertexBuffers, int Shapes, int ShapeDict, int Materials, int MaterialDict, int UserArray, int UserDict);
}
