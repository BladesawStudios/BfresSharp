using BfresSharp.Errors;
using BfresSharp.Model.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes FMAA sections in the Switch layout.</summary>
/// <remarks>
/// Version 9 and later put the flags straight after the magic (112 bytes); earlier files have 12 unused bytes
/// after the magic and the flags with the counts (120 bytes). The header is written with the other headers of its kind;
/// <see cref="WriteBody"/> then writes what it points to: bind indices, the per-material records, the texture bindings
/// and names, the entries and curves of each material, and user data.
/// </remarks>
public static class MaterialAnimCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x70 : 0x78;

    public readonly record struct Slots(int BindIndices, int Items, int TextureUnknown, int TextureNames, int UserArray, int UserDict, int TextureBinds);

    public static MaterialAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        bool modern = reader.Profile.Modern;
        long fields = at + (modern ? 8 : 0x10);
        long c = fields + 0x50;
        var anim = new MaterialAnim
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Flags = view.U16(modern ? at + 4 : c),
            Pad = modern ? view.U16(at + 6) : (ushort)0,
            FrameCount = view.I32(modern ? c : c + 8),
            BakedSize = view.U32(modern ? c + 4 : c + 12),
        };
        if (reader.Pointer(fields + 0x10) != 0) throw new BfresUnsupportedException("A bound model inside an animation is not supported yet", fields + 0x10);

        int items = view.U16(modern ? c + 0xA : c + 4);
        int textures = view.U16(modern ? c + 0x14 : c + 0x16);
        long bindIndices = reader.Pointer(fields + 0x18), names = reader.Pointer(fields + 0x30), binds = reader.Pointer(fields + 0x48);
        anim.Items = reader.ReadArray(reader.Pointer(fields + 0x20), items, MaterialAnimItemCodec.Size, offset => MaterialAnimItemCodec.Read(reader, offset)).ToList();
        anim.UserData = UserDataCodec.ReadDict(reader, fields + 0x40, fields + 0x38);
        if (bindIndices != 0) anim.BindIndices = reader.ReadArray(bindIndices, items, 2, offset => view.U16(offset));
        if (names != 0) anim.TextureNames = reader.ReadArray(names, textures, 8, offset => reader.String(offset) ?? "").ToList();
        if (binds != 0) anim.TextureBinds = reader.ReadArray(binds, textures, 8, offset => (long)view.U64(offset));
        return anim;
    }

    public static Slots WriteHeader(SwitchWriter writer, MaterialAnim anim)
    {
        bool modern = writer.Profile.Modern;
        writer.Out.Bytes("FMAA"u8);
        if (modern) writer.Out.U16(anim.Flags).U16(anim.Pad);
        else writer.Out.Zeros(12);
        writer.StringPointer(anim.Name);
        writer.StringPointer(anim.Path);
        writer.Unlisted();
        bool textured = anim.TextureNames.Count > 0;
        int bindIndices = writer.Pointer(), itemArray = writer.Pointer(), unknown = writer.Pointer();
        int names = textured ? writer.Pointer() : writer.Unlisted();
        int userArray = anim.UserData.Count > 0 || modern ? writer.Pointer() : writer.Unlisted();
        int userDict = writer.Pointer();
        var slots = new Slots(bindIndices, itemArray, unknown, names, userArray, userDict, textured ? writer.Pointer() : writer.Unlisted());
        ushort users = (ushort)anim.UserData.Count, items = (ushort)anim.Items.Count;
        ushort curves = (ushort)anim.Items.Sum(item => item.Curves.Count);
        if (modern) writer.Out.U32((uint)anim.FrameCount).U32(anim.BakedSize).U16(users).U16(items).U16(curves);
        else writer.Out.U16(anim.Flags).U16(users).U16(items).U16(curves).U32((uint)anim.FrameCount).U32(anim.BakedSize);
        writer.Out.U16((ushort)anim.Items.Sum(item => item.ParamCount)).U16((ushort)anim.Items.Sum(item => item.PatternCount))
            .U16(modern ? (ushort)anim.Items.Sum(item => item.VisualCount) : (ushort)0).U16((ushort)anim.TextureNames.Count);
        if (modern) writer.Out.U16(0);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, MaterialAnim anim, Slots slots)
    {
        if (anim.BindIndices.Length > 0)
        {
            writer.Align(8).Resolve(slots.BindIndices);
            foreach (ushort index in anim.BindIndices) writer.Out.U16(index);
            writer.Align(8);
        }

        var itemSlots = new List<MaterialAnimItemCodec.Slots>();
        if (anim.Items.Count > 0)
        {
            writer.Align(8).Resolve(slots.Items);
            foreach (MaterialAnimItem item in anim.Items) itemSlots.Add(MaterialAnimItemCodec.WriteHeader(writer, item));
        }

        WriteTextures(writer, anim, slots);
        for (int index = 0; index < anim.Items.Count; index++) MaterialAnimItemCodec.WriteBody(writer, anim.Items[index], itemSlots[index]);
        int[] valueSlots = anim.UserData.Count > 0 ? UserDataCodec.WriteArray(writer, anim.UserData, slots.UserArray) : [];
        UserDataCodec.WriteDictAndValues(writer, anim.UserData, slots.UserDict, valueSlots);
    }

    // The texture bindings, the runtime table and the names; without textures only the table is pointed at.
    private static void WriteTextures(SwitchWriter writer, MaterialAnim anim, Slots slots)
    {
        if (anim.TextureNames.Count > 0)
        {
            writer.Align(8).Resolve(slots.TextureBinds);
            foreach (long bind in anim.TextureBinds) writer.Out.U64((ulong)bind);
        }

        if (anim.Items.Count == 0) return;
        if (anim.TextureNames.Count == 0 && writer.Profile.Modern) return;
        writer.Align(8).Resolve(slots.TextureUnknown);
        if (anim.TextureNames.Count == 0) return;
        writer.Out.Zeros(anim.TextureNames.Count * 8);
        writer.Resolve(slots.TextureNames);
        foreach (string name in anim.TextureNames) writer.StringPointer(name);
    }
}
