using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes material animations in the Wii U layout: parameters, colors and texture transforms share one, texture patterns have another.</summary>
/// <remarks>
/// A parameter animation (FSHU, 52 bytes from version 3.4) holds the name, path and flags, the frame count, the numbers of
/// materials and user data, of parameter entries and of curves, the baked size, then offsets to the bound model, the bind
/// indices, the material items and the user data. A pattern animation (FTXP, 56 bytes) counts texture names and pattern
/// entries instead and adds a dictionary of the texture names, which it places after the data of its items.
/// </remarks>
public static class MaterialAnimCodec
{
    private const uint MinVersion = 0x03040000;

    public static MaterialAnim ReadParam(WiiUReader reader, long at)
    {
        CheckSupported(reader, at);
        var view = reader.View;
        var anim = ReadCommon(reader, at);
        anim.Items = reader.ReadList(at + 0x2C, view.U16(at + 0x14), MaterialAnimItemCodec.ParamItemSize, offset => MaterialAnimItemCodec.ReadParam(reader, offset));
        anim.UserData = UserDataCodec.ReadDict(reader, at + 0x30);
        return anim;
    }

    public static MaterialAnim ReadPattern(WiiUReader reader, long at)
    {
        CheckSupported(reader, at);
        var view = reader.View;
        var anim = ReadCommon(reader, at);
        anim.Items = reader.ReadList(at + 0x2C, view.U16(at + 0x16), MaterialAnimItemCodec.PatternItemSize, offset => MaterialAnimItemCodec.ReadPattern(reader, offset));
        anim.TextureNames = reader.ReadDict(at + 0x30, offset => reader.String(offset) ?? "").Values.ToList();
        anim.UserData = UserDataCodec.ReadDict(reader, at + 0x34);
        return anim;
    }

    public static void WriteParam(WiiUWriter writer, MaterialAnim anim)
    {
        var o = writer.Out;
        o.Bytes("FSHU"u8);
        WriteCommon(writer, anim);
        o.U16((ushort)anim.Items.Count).U16((ushort)anim.UserData.Count).U32((uint)anim.Items.Sum(item => item.ParamCount))
            .U32((uint)anim.Items.Sum(item => item.Curves.Count)).U32(anim.BakedSize).U32(0);
        WriteTail(writer, anim, MaterialAnimItemCodec.WriteParam, out object bindIndices);
        UserDataCodec.WriteDict(writer, anim.UserData);
        writer.Place(bindIndices, anim.Items);
        foreach (MaterialAnimItem item in anim.Items) MaterialAnimItemCodec.PlaceParamChildren(writer, item);
        writer.PlaceAll(anim.UserData);
    }

    public static void WritePattern(WiiUWriter writer, MaterialAnim anim)
    {
        var o = writer.Out;
        o.Bytes("FTXP"u8);
        WriteCommon(writer, anim);
        o.U16((ushort)anim.TextureNames.Count).U16((ushort)anim.Items.Count).U32((uint)anim.Items.Sum(item => item.PatternAnimInfos.Count))
            .U32((uint)anim.Items.Sum(item => item.Curves.Count)).U32(anim.BakedSize).U32(0);
        WriteTail(writer, anim, MaterialAnimItemCodec.WritePattern, out object bindIndices);
        var names = new ResDict<TextureRef>();
        foreach (string name in anim.TextureNames) names.Add(name, new TextureRef(name));
        writer.Dict(names, (reference, _) =>
        {
            writer.Name(reference.Name);
            o.U32(0);
        });
        UserDataCodec.WriteDict(writer, anim.UserData);
        writer.Place(bindIndices, anim.Items);
        writer.PlaceValues(names);
        foreach (MaterialAnimItem item in anim.Items) MaterialAnimItemCodec.PlacePatternChildren(writer, item);
        writer.Place(names);
        writer.PlaceAll(anim.UserData);
    }

    private sealed record TextureRef(string Name);

    private static void CheckSupported(WiiUReader reader, long at)
    {
        if (!reader.AtLeast(MinVersion)) throw new BfresFormatException("Material animations before version 3.4 are not supported yet", at);
        if (reader.Offset(at + 0x24) != 0) throw new BfresFormatException("A bound model inside an animation is not supported yet", at + 0x24);
    }

    private static MaterialAnim ReadCommon(WiiUReader reader, long at)
    {
        var view = reader.View;
        uint flags = view.U32(at + 0xC);
        var anim = new MaterialAnim
        {
            Name = reader.String(at + 4) ?? "",
            Path = reader.String(at + 8) ?? "",
            Flags = (ushort)flags,
            Pad = (ushort)(flags >> 16),
            FrameCount = view.I32(at + 0x10),
            BakedSize = view.U32(at + 0x20),
        };
        long bind = reader.Offset(at + 0x28);
        int count = view.U16(at + (view.U32(at) == 0x46545850 ? 0x16 : 0x14));
        if (bind != 0) anim.BindIndices = Enumerable.Range(0, count).Select(index => view.U16(bind + index * 2L)).ToArray();
        return anim;
    }

    private static void WriteCommon(WiiUWriter writer, MaterialAnim anim)
    {
        writer.Name(anim.Name);
        writer.Name(anim.Path);
        writer.Out.U32((uint)anim.Pad << 16 | anim.Flags).U32((uint)anim.FrameCount);
    }

    private static void WriteTail(WiiUWriter writer, MaterialAnim anim, Action<WiiUWriter, MaterialAnimItem> writeItem, out object bindIndices)
    {
        var o = writer.Out;
        bindIndices = writer.Array(() =>
        {
            foreach (ushort index in anim.BindIndices) o.U16(index);
        });
        writer.List(anim.Items, (item, _) => writeItem(writer, item), pointWhenEmpty: true);
    }
}
