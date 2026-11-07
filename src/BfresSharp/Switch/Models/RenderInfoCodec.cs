using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes render infos in the Switch layout, which is 24 bytes.</summary>
/// <remarks>
/// The name pointer, a pointer to the values, the value count, the type and five unused bytes. The values are
/// integers, floats or name pointers and are stored apart from the records.
/// </remarks>
public static class RenderInfoCodec
{
    public const int Size = 24;

    public static RenderInfo Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var info = new RenderInfo { Name = reader.String(at) ?? "" };
        ReadValues(reader, info, (RenderInfoType)view.U8(at + 0x12), reader.Pointer(at + 8), view.U16(at + 0x10));
        return info;
    }

    // Reads the count values of a render info, which are integers, floats or name pointers.
    public static void ReadValues(SwitchReader reader, RenderInfo info, RenderInfoType type, long data, int count)
    {
        var view = reader.View;
        switch (type)
        {
            case RenderInfoType.Int32:
                info.SetValue(data == 0 ? [] : reader.ReadArray(data, count, 4, offset => view.I32(offset)));
                break;
            case RenderInfoType.Single:
                info.SetValue(data == 0 ? [] : reader.ReadArray(data, count, 4, offset => view.F32(offset)));
                break;
            default:
                info.SetValue(data == 0 ? [] : reader.ReadArray(data, count, 8, offset => reader.String(offset) ?? ""));
                break;
        }
    }

    // Writes the record and returns the slot of its value pointer.
    public static int WriteRecord(SwitchWriter writer, RenderInfo info)
    {
        writer.StringPointer(info.Name);
        int slot = info.Count == 0 ? writer.Unlisted() : writer.Pointer();
        writer.Out.U16((ushort)info.Count).U8((byte)info.Type).Zeros(5);
        return slot;
    }

    // The values come after the records in three runs: all strings, then all integers, then all floats.
    public static void WriteValues(SwitchWriter writer, IReadOnlyList<RenderInfo> infos, IReadOnlyList<int> slots)
    {
        foreach (RenderInfoType type in new[] { RenderInfoType.String, RenderInfoType.Int32, RenderInfoType.Single })
        {
            for (int index = 0; index < infos.Count; index++)
            {
                RenderInfo info = infos[index];
                if (info.Type != type || info.Count == 0) continue;
                writer.Align(type == RenderInfoType.String ? 8 : 4).Resolve(slots[index]);
                WriteValue(writer, info);
            }
        }

        writer.Align(8);
    }

    // The size in bytes of a render info's values: 8 for a name pointer and 4 for an integer or float.
    public static int ValueLength(RenderInfo info) => info.Count * (info.Type == RenderInfoType.String ? 8 : 4);

    // Version 10 packs the values of all render infos into a block of the given size, each at its own offset.
    public static void WriteBlock(SwitchWriter writer, IReadOnlyList<RenderInfo> infos, int size)
    {
        int start = writer.Position;
        writer.Out.Zeros(size);
        foreach (RenderInfo info in infos)
        {
            int at = start + info.DataOffset;
            switch (info.Type)
            {
                case RenderInfoType.Int32: for (int index = 0; index < info.Ints.Length; index++) writer.Out.SetU32(at + index * 4, (uint)info.Ints[index]); break;
                case RenderInfoType.Single: for (int index = 0; index < info.Floats.Length; index++) writer.Out.SetU32(at + index * 4, BitConverter.SingleToUInt32Bits(info.Floats[index])); break;
                default: for (int index = 0; index < info.Strings.Length; index++) writer.StringPointerAt(at + index * 8, info.Strings[index]); break;
            }
        }
    }

    // Places the values of render infos that have no offset yet: name pointers first, then the rest, in list order.
    public static void PlaceValues(IReadOnlyList<RenderInfo> infos)
    {
        if (infos.All(info => info.DataOffset >= 0)) return;
        int offset = 0;
        foreach (RenderInfo info in infos.Where(info => info.Type == RenderInfoType.String).Concat(infos.Where(info => info.Type != RenderInfoType.String)))
        {
            info.DataOffset = offset;
            offset += ValueLength(info);
        }
    }

    private static void WriteValue(SwitchWriter writer, RenderInfo info)
    {
        switch (info.Type)
        {
            case RenderInfoType.Int32: foreach (int value in info.Ints) writer.Out.U32((uint)value); break;
            case RenderInfoType.Single: foreach (float value in info.Floats) writer.Out.F32(value); break;
            default: foreach (string value in info.Strings) writer.StringPointer(value); break;
        }
    }
}
