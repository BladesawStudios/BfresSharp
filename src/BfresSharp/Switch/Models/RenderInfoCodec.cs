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
        long data = reader.Pointer(at + 8);
        int count = view.U16(at + 0x10);
        switch ((RenderInfoType)view.U8(at + 0x12))
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

        return info;
    }

    // Writes the record and returns the slot of its value pointer.
    public static int WriteRecord(SwitchWriter writer, RenderInfo info)
    {
        writer.StringPointer(info.Name);
        int slot = writer.Pointer();
        writer.Out.U16((ushort)info.Count).U8((byte)info.Type).Zeros(5);
        return slot;
    }

    public static void WriteValues(SwitchWriter writer, RenderInfo info, int slot)
    {
        if (info.Count == 0) return;
        writer.Align(8).Resolve(slot);
        switch (info.Type)
        {
            case RenderInfoType.Int32: foreach (int value in info.Ints) writer.Out.U32((uint)value); break;
            case RenderInfoType.Single: foreach (float value in info.Floats) writer.Out.F32(value); break;
            default: foreach (string value in info.Strings) writer.StringPointer(value); break;
        }
    }
}
