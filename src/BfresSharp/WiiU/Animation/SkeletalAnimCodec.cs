using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes skeletal animations in the Wii U layout, which is 48 bytes from version 3.4.</summary>
/// <remarks>
/// The magic, name, path and flags, the frame count, the numbers of bone animations and user data, the number of curves and
/// the baked size, then offsets to the bone animations, the bound skeleton, the bind indices and the user data. The bind
/// indices, then the bone animations with the data of each in turn, follow the record.
/// </remarks>
public static class SkeletalAnimCodec
{
    private const uint MinVersion = 0x03040000;

    public static SkeletalAnim Read(WiiUReader reader, long at)
    {
        if (!reader.AtLeast(MinVersion)) throw new BfresFormatException("Skeletal animations before version 3.4 are not supported yet", at);
        var view = reader.View;
        if (reader.Offset(at + 0x24) != 0) throw new BfresFormatException("A bound skeleton inside an animation is not supported yet", at + 0x24);
        int boneCount = view.U16(at + 0x14);
        var anim = new SkeletalAnim
        {
            Name = reader.String(at + 4) ?? "",
            Path = reader.String(at + 8) ?? "",
            Flags = view.U32(at + 0xC),
            FrameCount = view.I32(at + 0x10),
            BakedSize = view.U32(at + 0x1C),
            BoneAnims = reader.ReadList(at + 0x20, boneCount, BoneAnimCodec.Size, offset => BoneAnimCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, at + 0x2C),
        };
        long bind = reader.Offset(at + 0x28);
        if (bind != 0) anim.BindIndices = Enumerable.Range(0, boneCount).Select(index => view.U16(bind + index * 2L)).ToArray();
        return anim;
    }

    public static void Write(WiiUWriter writer, SkeletalAnim anim)
    {
        var o = writer.Out;
        o.Bytes("FSKA"u8);
        writer.Name(anim.Name);
        writer.Name(anim.Path);
        o.U32(anim.Flags).U32((uint)anim.FrameCount).U16((ushort)anim.BoneAnims.Count).U16((ushort)anim.UserData.Count)
            .U32((uint)anim.BoneAnims.Sum(bone => bone.Curves.Count)).U32(anim.BakedSize);
        writer.List(anim.BoneAnims, (bone, _) => BoneAnimCodec.Write(writer, bone));
        o.U32(0);
        writer.Custom(anim.BindIndices.Length == 0 ? null : anim.BindIndices, () =>
        {
            foreach (ushort index in anim.BindIndices) o.U16(index);
        });
        UserDataCodec.WriteDict(writer, anim.UserData);
        writer.Place(anim.BindIndices, anim.BoneAnims);
        foreach (BoneAnim bone in anim.BoneAnims) BoneAnimCodec.PlaceChildren(writer, bone);
        writer.PlaceAll(anim.UserData);
    }
}
