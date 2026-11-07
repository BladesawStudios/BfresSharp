using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes bone and material visibility animations in the Wii U layout, which is 52 bytes from version 3.4.</summary>
/// <remarks>
/// The magic, name and path, the flags and the number of user data, the frame count, the numbers of animated names and of
/// curves, the baked size, then offsets to the bound model, the bind indices, the names, the curves, the base visibility
/// bits and the user data. The names are an array of name offsets, and the bits are packed 32 to a word, lowest first.
/// </remarks>
public static class VisibilityAnimCodec
{
    private const uint MinVersion = 0x03040000;

    public static VisibilityAnim Read(WiiUReader reader, long at)
    {
        if (!reader.AtLeast(MinVersion)) throw new BfresUnsupportedException("Visibility animations before version 3.4 are not supported yet", at);
        if (reader.Offset(at + 0x1C) != 0) throw new BfresUnsupportedException("A bound model inside an animation is not supported yet", at + 0x1C);
        var view = reader.View;
        int count = view.U16(at + 0x14);
        var anim = new VisibilityAnim
        {
            Name = reader.String(at + 4) ?? "",
            Path = reader.String(at + 8) ?? "",
            Flags = view.U16(at + 0xC),
            FrameCount = view.I32(at + 0x10),
            BakedSize = view.U32(at + 0x18),
            Curves = CurveCodec.ReadList(reader, at + 0x28, view.U16(at + 0x16)),
            UserData = UserDataCodec.ReadDict(reader, at + 0x30),
        };
        long bind = reader.Offset(at + 0x20), names = reader.Offset(at + 0x24), bits = reader.Offset(at + 0x2C);
        if (bind != 0) anim.BindIndices = Enumerable.Range(0, count).Select(index => view.U16(bind + index * 2L)).ToArray();
        if (names != 0) anim.Names = Enumerable.Range(0, count).Select(index => reader.String(names + index * 4L) ?? "").ToList();
        if (bits != 0) anim.BaseData = Enumerable.Range(0, count).Select(index => ((view.U32(bits + index / 32 * 4L) >> (index % 32)) & 1) != 0).ToArray();
        return anim;
    }

    public static void Write(WiiUWriter writer, VisibilityAnim anim)
    {
        var o = writer.Out;
        o.Bytes("FVIS"u8);
        writer.Name(anim.Name);
        writer.Name(anim.Path);
        o.U16(anim.Flags).U16((ushort)anim.UserData.Count).U32((uint)anim.FrameCount).U16((ushort)anim.Names.Count).U16((ushort)anim.Curves.Count)
            .U32(anim.BakedSize).U32(0);
        object bind = writer.Array(() =>
        {
            foreach (ushort index in anim.BindIndices) o.U16(index);
        });
        object names = writer.Array(() =>
        {
            foreach (string name in anim.Names) writer.Name(name);
        });
        CurveCodec.WriteList(writer, anim.Curves);
        object bits = writer.Array(() => WriteBits(o, anim.BaseData));
        UserDataCodec.WriteDict(writer, anim.UserData);
        writer.Place(bind, names, anim.Curves, bits);
        CurveCodec.PlaceData(writer, anim.Curves);
        writer.PlaceAll(anim.UserData);
    }

    private static void WriteBits(Binary.ByteBuilder o, bool[] bits)
    {
        var words = new uint[(bits.Length + 31) / 32];
        for (int index = 0; index < bits.Length; index++)
        {
            if (bits[index]) words[index / 32] |= 1u << (index % 32);
        }

        foreach (uint word in words) o.U32(word);
    }
}
