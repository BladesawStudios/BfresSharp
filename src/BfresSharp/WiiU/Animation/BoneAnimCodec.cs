using System.Numerics;
using BfresSharp.Errors;
using BfresSharp.Model.Animation;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes the animation of one bone in the Wii U layout, which is 24 bytes.</summary>
/// <remarks>
/// The flags, the name, the offsets into the transform of the curves, the number of curves and the index of the first, then
/// offsets to the curves and to the base values. The index of the first curve is a little-endian word, though the file is big endian. The base values come first, then the curves and their data.
/// </remarks>
public static class BoneAnimCodec
{
    public const int Size = 0x18;

    public static BoneAnim Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        var bone = new BoneAnim
        {
            Flags = view.U32(at),
            Name = reader.String(at + 4) ?? "",
            BeginRotate = view.U8(at + 8),
            BeginTranslate = view.U8(at + 9),
            BeginBaseTranslate = view.U8(at + 0xB),
            BeginCurve = view.U8(at + 0xC) | view.U8(at + 0xD) << 8 | view.U8(at + 0xE) << 16 | view.U8(at + 0xF) << 24,
        };
        bone.Curves = CurveCodec.ReadList(reader, at + 0x10, view.U8(at + 0xA));
        long baseData = reader.Offset(at + 0x14);
        if (baseData != 0) bone.BaseData = ReadBase(reader, baseData, bone.FlagsBase);
        return bone;
    }

    public static void Write(WiiUWriter writer, BoneAnim bone)
    {
        if (bone.Curves.Count > byte.MaxValue) throw new BfresFormatException("A bone has more than 255 curves");
        var o = writer.Out;
        o.U32(bone.Flags);
        writer.Name(bone.Name);
        o.U8(bone.BeginRotate).U8(bone.BeginTranslate).U8((byte)bone.Curves.Count).U8(bone.BeginBaseTranslate).U8((byte)bone.BeginCurve).U8((byte)(bone.BeginCurve >> 8)).U8((byte)(bone.BeginCurve >> 16)).U8((byte)(bone.BeginCurve >> 24));
        CurveCodec.WriteList(writer, bone.Curves, pointWhenEmpty: bone.BaseData is not null);
        writer.Item(bone.BaseData, -1, (data, _) => WriteBase(writer, data, bone.FlagsBase));
    }

    // Places what a bone animation refers to: the base values, the curves, and the frames and keys of each curve.
    public static void PlaceChildren(WiiUWriter writer, BoneAnim bone)
    {
        writer.Place(bone.BaseData, bone.Curves);
        CurveCodec.PlaceData(writer, bone.Curves);
    }

    private static BoneAnimData ReadBase(WiiUReader reader, long at, BoneAnimFlagsBase flags)
    {
        var view = reader.View;
        var data = new BoneAnimData();
        if (flags.HasFlag(BoneAnimFlagsBase.Scale))
        {
            data.Scale = new Vector3(view.F32(at), view.F32(at + 4), view.F32(at + 8));
            at += 12;
        }

        if (flags.HasFlag(BoneAnimFlagsBase.Rotate))
        {
            data.Rotate = new Vector4(view.F32(at), view.F32(at + 4), view.F32(at + 8), view.F32(at + 12));
            at += 16;
        }

        if (flags.HasFlag(BoneAnimFlagsBase.Translate)) data.Translate = new Vector3(view.F32(at), view.F32(at + 4), view.F32(at + 8));
        return data;
    }

    private static void WriteBase(WiiUWriter writer, BoneAnimData data, BoneAnimFlagsBase flags)
    {
        var o = writer.Out;
        if (flags.HasFlag(BoneAnimFlagsBase.Scale)) o.F32(data.Scale.X).F32(data.Scale.Y).F32(data.Scale.Z);
        if (flags.HasFlag(BoneAnimFlagsBase.Rotate)) o.F32(data.Rotate.X).F32(data.Rotate.Y).F32(data.Rotate.Z).F32(data.Rotate.W);
        if (flags.HasFlag(BoneAnimFlagsBase.Translate)) o.F32(data.Translate.X).F32(data.Translate.Y).F32(data.Translate.Z);
    }
}
