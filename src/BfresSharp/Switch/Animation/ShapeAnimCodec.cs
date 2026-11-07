using BfresSharp.Model.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes FSHA sections in the Switch layout.</summary>
/// <remarks>
/// Version 9 and later put the flags after the magic (80 bytes); earlier files have a linked header block, the flags
/// after the pointers and padding (96 bytes). The header holds the name and path, an unused bound model pointer, the bind
/// indices, the vertex shape animations and the user data, then the counts. <see cref="VertexShapeAnimCodec"/> reads
/// and writes the entries.
/// </remarks>
public static class ShapeAnimCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x50 : 0x60;

    public static ShapeAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        bool modern = reader.Profile.Modern;
        long fields = at + (modern ? 8 : 0x10);
        long counts = fields + 0x38 + (modern ? 0 : 2);
        var anim = new ShapeAnim
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Flags = modern ? view.U32(at + 4) : view.U16(fields + 0x38),
            FrameCount = view.I32(modern ? counts : counts + 6),
            BakedSize = view.U32(modern ? counts + 4 : counts + 10),
        };
        int vertexShapes = view.U16(modern ? counts + 10 : counts + 2);
        long bindIndices = reader.Pointer(fields + 0x18);
        long entries = reader.Pointer(fields + 0x20);
        anim.UserData = UserDataCodec.ReadDict(reader, fields + 0x30, fields + 0x28);
        if (bindIndices != 0) anim.BindIndices = reader.ReadArray(bindIndices, vertexShapes, 2, offset => view.U16(offset));
        anim.VertexShapeAnims = reader.ReadArray(entries, vertexShapes, VertexShapeAnimCodec.Size, offset => VertexShapeAnimCodec.Read(reader, offset)).ToList();
        return anim;
    }

    public static Slots WriteHeader(SwitchWriter writer, ShapeAnim anim)
    {
        bool modern = writer.Profile.Modern;
        writer.Out.Bytes("FSHA"u8);
        if (modern) writer.Out.U32(anim.Flags);
        else writer.Block();
        writer.StringPointer(anim.Name);
        writer.StringPointer(anim.Path);
        writer.Unlisted();
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        if (!modern) writer.Out.U16((ushort)anim.Flags);
        ushort keyShapes = (ushort)anim.VertexShapeAnims.Sum(shape => shape.KeyShapeAnimInfos.Count);
        ushort curves = (ushort)anim.VertexShapeAnims.Sum(shape => shape.Curves.Count);
        if (modern) writer.Out.U32((uint)anim.FrameCount).U32(anim.BakedSize).U16((ushort)anim.UserData.Count).U16((ushort)anim.VertexShapeAnims.Count).U16(keyShapes).U16(curves);
        else writer.Out.U16((ushort)anim.UserData.Count).U16((ushort)anim.VertexShapeAnims.Count).U16(keyShapes).U32((uint)anim.FrameCount).U32(anim.BakedSize).U16(curves);
        writer.Align(8);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, ShapeAnim anim, Slots slots)
    {
        if (anim.BindIndices.Length > 0)
        {
            writer.Align(8).Resolve(slots.BindIndices);
            foreach (ushort index in anim.BindIndices) writer.Out.U16(index);
        }

        var headers = new List<VertexShapeAnimCodec.Slots>();
        if (anim.VertexShapeAnims.Count > 0)
        {
            writer.Align(8).Resolve(slots.Entries);
            foreach (VertexShapeAnim shape in anim.VertexShapeAnims) headers.Add(VertexShapeAnimCodec.WriteHeader(writer, shape));
        }

        int[] valueSlots = UserDataCodec.WriteArray(writer, anim.UserData, slots.UserArray);
        for (int index = 0; index < headers.Count; index++) VertexShapeAnimCodec.WriteData(writer, anim.VertexShapeAnims[index], headers[index]);
        UserDataCodec.WriteDictAndValues(writer, anim.UserData, slots.UserDict, valueSlots);
    }

    public readonly record struct Slots(int BindIndices, int Entries, int UserArray, int UserDict);
}
