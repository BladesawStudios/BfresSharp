using BfresSharp.Model.Material;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes render infos in the Wii U layout.</summary>
/// <remarks>
/// The count, the type, a pad byte and the name, then the values inline: integers and floats as words, names as offsets.
/// Numeric values are followed by one more zero word.
/// </remarks>
public static class RenderInfoCodec
{
    public static RenderInfo Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        int count = view.U16(at);
        var info = new RenderInfo { Name = reader.String(at + 4) ?? "" };
        long values = at + 8;
        switch ((RenderInfoType)view.U8(at + 2))
        {
            case RenderInfoType.Int32: info.SetValue(Enumerable.Range(0, count).Select(index => view.I32(values + index * 4L)).ToArray()); break;
            case RenderInfoType.Single: info.SetValue(Enumerable.Range(0, count).Select(index => view.F32(values + index * 4L)).ToArray()); break;
            default: info.SetValue(Enumerable.Range(0, count).Select(index => reader.String(values + index * 4L) ?? "").ToArray()); break;
        }

        return info;
    }

    public static void Write(WiiUWriter writer, RenderInfo info)
    {
        var o = writer.Out;
        o.U16((ushort)info.Count).U8((byte)info.Type).U8(0);
        writer.Name(info.Name);
        switch (info.Type)
        {
            case RenderInfoType.Int32:
                foreach (int value in info.Ints) o.U32((uint)value);
                o.U32(0);
                break;
            case RenderInfoType.Single:
                foreach (float value in info.Floats) o.F32(value);
                o.U32(0);
                break;
            default:
                foreach (string value in info.Strings) writer.Name(value);
                if (info.Strings.Length == 0) o.U32(0);
                break;
        }
    }
}
