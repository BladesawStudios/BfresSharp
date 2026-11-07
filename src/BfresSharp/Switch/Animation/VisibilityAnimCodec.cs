using BfresSharp.Model.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes bone visibility animations in the Switch layout.</summary>
/// <remarks>
/// The header is 96 bytes in version 9 and later and 104 before. It holds the name and path, an unused bound model
/// pointer, pointers to the bind indices, curves, base data and names, the user data, the two spare words, the frame
/// count, the name and curve counts and the baked size. The base data is one bit a name, packed 32 to a word.
/// </remarks>
public static class VisibilityAnimCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x60 : 0x68;

    public static VisibilityAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        bool modern = reader.Profile.Modern;
        long fields = at + (modern ? 8 : 0x10);
        long counts = fields + 0x48;
        var anim = new VisibilityAnim
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Flags = view.U16(modern ? at + 4 : counts),
            Pad = modern ? view.U16(at + 6) : (ushort)0,
            Spare = modern ? view.U16(counts) : (ushort)0,
            FrameCount = view.I32(counts + 4),
            BakedSize = view.U32(counts + 12),
        };
        int names = view.U16(counts + 8);
        int curves = view.U16(counts + 10);
        long bindIndices = reader.Pointer(fields + 0x18);
        long curveArray = reader.Pointer(fields + 0x20);
        long baseData = reader.Pointer(fields + 0x28);
        long nameArray = reader.Pointer(fields + 0x30);
        anim.UserData = UserDataCodec.ReadDict(reader, fields + 0x40, fields + 0x38);
        if (bindIndices != 0) anim.BindIndices = reader.ReadArray(bindIndices, names, 2, offset => view.U16(offset));
        if (nameArray != 0) anim.Names = reader.ReadArray(nameArray, names, 8, offset => reader.String(offset) ?? "").ToList();
        if (curveArray != 0) anim.Curves = CurveCodec.ReadList(reader, curveArray, curves);
        if (baseData != 0) anim.BaseData = ReadBits(reader, baseData, names);
        return anim;
    }

    public static Slots WriteHeader(SwitchWriter writer, VisibilityAnim anim)
    {
        bool modern = writer.Profile.Modern;
        writer.Out.Bytes("FBVS"u8);
        if (modern) writer.Out.U16(anim.Flags).U16(anim.Pad);
        else writer.Block();
        writer.StringPointer(anim.Name);
        writer.StringPointer(anim.Path);
        writer.Unlisted();
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        writer.Out.U16(modern ? anim.Spare : anim.Flags).U16((ushort)anim.UserData.Count).U32((uint)anim.FrameCount)
            .U16((ushort)anim.Names.Count).U16((ushort)anim.Curves.Count).U32(anim.BakedSize);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, VisibilityAnim anim, Slots slots)
    {
        if (anim.BindIndices.Length > 0)
        {
            writer.Align(8).Resolve(slots.BindIndices);
            foreach (ushort index in anim.BindIndices) writer.Out.U16(index);
            writer.Align(8);
        }

        if (anim.Names.Count > 0)
        {
            writer.Resolve(slots.Names);
            foreach (string name in anim.Names) writer.StringPointer(name);
            writer.Align(8);
        }

        var curveSlots = CurveCodec.WriteHeaders(writer, anim.Curves, slots.Curves);
        if (anim.BaseData.Length > 0)
        {
            writer.Align(8).Resolve(slots.BaseData);
            WriteBits(writer, anim.BaseData);
            writer.Align(8);
        }

        int[] valueSlots = UserDataCodec.WriteArray(writer, anim.UserData, slots.UserArray);
        CurveCodec.WriteData(writer, anim.Curves, curveSlots);
        UserDataCodec.WriteDictAndValues(writer, anim.UserData, slots.UserDict, valueSlots);
    }

    private static bool[] ReadBits(SwitchReader reader, long at, int count)
    {
        var bits = new bool[count];
        for (int index = 0; index < count; index++) bits[index] = ((reader.View.U32(at + index / 32 * 4L) >> (index % 32)) & 1) != 0;
        return bits;
    }

    private static void WriteBits(SwitchWriter writer, bool[] bits)
    {
        var words = new uint[(bits.Length + 31) / 32];
        for (int index = 0; index < bits.Length; index++)
        {
            if (bits[index]) words[index / 32] |= 1u << (index % 32);
        }

        foreach (uint word in words) writer.Out.U32(word);
    }

    public readonly record struct Slots(int BindIndices, int Curves, int BaseData, int Names, int UserArray, int UserDict);
}
