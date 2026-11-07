using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes FSKA sections in the Switch layout.</summary>
/// <remarks>
/// Version 9 and later put the flags straight after the magic and the counts at the end (80 bytes); earlier files have
/// a linked header block after the magic, the flags after the pointers and a trailing padding word (96 bytes). The
/// header is written with the other headers of its kind; <see cref="WriteBody"/> then writes what it points to: bind
/// indices, bone animations with their base data and curves, and user data.
/// </remarks>
public static class SkeletalAnimCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x50 : 0x60;

    public static SkeletalAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        bool modern = reader.Profile.Modern;
        long fields = at + (modern ? 8 : 0x10);
        long counts = at + (modern ? 0x40 : 0x4C);
        var anim = new SkeletalAnim
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Flags = view.U32(modern ? at + 4 : at + 0x48),
            FrameCount = view.I32(counts),
            BakedSize = view.U32(counts + 8),
        };
        if (reader.Pointer(fields + 0x10) != 0) throw new BfresUnsupportedException("A bound skeleton inside an animation is not supported yet", fields + 0x10);

        int boneCount = view.U16(counts + 12);
        long bindIndices = reader.Pointer(fields + 0x18);
        long boneAnims = reader.Pointer(fields + 0x20);
        anim.UserData = UserDataCodec.ReadDict(reader, fields + 0x30, fields + 0x28);
        anim.BoneAnims = reader.ReadArray(boneAnims, boneCount, BoneAnimCodec.Size(reader.Profile), offset => BoneAnimCodec.Read(reader, offset)).ToList();
        anim.BindIndices = bindIndices == 0 ? [] : reader.ReadArray(bindIndices, boneCount, 2, offset => view.U16(offset));
        return anim;
    }

    public static Slots WriteHeader(SwitchWriter writer, SkeletalAnim anim)
    {
        bool modern = writer.Profile.Modern;
        writer.Out.Bytes("FSKA"u8);
        if (modern) writer.Out.U32(anim.Flags);
        else writer.Block();
        writer.StringPointer(anim.Name);
        writer.StringPointer(anim.Path);
        writer.Unlisted();
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        if (!modern) writer.Out.U32(anim.Flags);
        writer.Out.U32((uint)anim.FrameCount).U32((uint)anim.BoneAnims.Sum(bone => bone.Curves.Count)).U32(anim.BakedSize)
            .U16((ushort)anim.BoneAnims.Count).U16((ushort)anim.UserData.Count);
        if (!modern) writer.Out.U32(0);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, SkeletalAnim anim, Slots slots)
    {
        if (anim.BindIndices.Length > 0)
        {
            writer.Align(8).Resolve(slots.BindIndices);
            foreach (ushort index in anim.BindIndices) writer.Out.U16(index);
        }

        var headers = new List<(int Curves, int Base)>();
        if (anim.BoneAnims.Count > 0)
        {
            writer.Align(8).Resolve(slots.BoneAnims);
            foreach (BoneAnim bone in anim.BoneAnims) headers.Add(BoneAnimCodec.WriteHeader(writer, bone));
        }

        int[] valueSlots = UserDataCodec.WriteArray(writer, anim.UserData, slots.UserArray);
        for (int index = 0; index < anim.BoneAnims.Count; index++) WriteBoneData(writer, anim.BoneAnims[index], headers[index]);
        UserDataCodec.WriteDictAndValues(writer, anim.UserData, slots.UserDict, valueSlots);
    }

    private static void WriteBoneData(SwitchWriter writer, BoneAnim bone, (int Curves, int Base) slots)
    {
        if (bone.BaseData is not null)
        {
            writer.Align(8).Resolve(slots.Base);
            BoneAnimCodec.WriteBase(writer, bone);
        }

        CurveCodec.WriteList(writer, bone.Curves, slots.Curves);
    }

    public readonly record struct Slots(int BindIndices, int BoneAnims, int UserArray, int UserDict);
}
