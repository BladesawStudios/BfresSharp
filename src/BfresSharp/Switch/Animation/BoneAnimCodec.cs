using System.Numerics;
using BfresSharp.Model.Animation;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes the bone animations of a skeletal animation in the Switch layout.</summary>
/// <remarks>
/// A bone animation is 56 bytes in version 9 and later and 40 before: name, curve array and base data pointers,
/// two reserved words (version 9 and later), the flags, three begin indices and a curve count, the begin-curve
/// index and padding. The base data holds the scale, rotation and translation the flags name, in that order.
/// </remarks>
public static class BoneAnimCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x38 : 0x28;

    public static BoneAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var bone = new BoneAnim { Name = reader.String(at) ?? "" };
        long curves = reader.Pointer(at + 8);
        long baseData = reader.Pointer(at + 0x10);
        long next = at + 0x18;
        if (reader.Profile.Modern)
        {
            bone.Reserved = [view.U64(next), view.U64(next + 8)];
            next += 16;
        }

        bone.Flags = view.U32(next);
        bone.BeginRotate = view.U8(next + 4);
        bone.BeginTranslate = view.U8(next + 5);
        int curveCount = view.U8(next + 6);
        bone.BeginBaseTranslate = view.U8(next + 7);
        bone.BeginCurve = view.I32(next + 8);
        if (baseData != 0) bone.BaseData = ReadBase(reader, baseData, bone.FlagsBase);
        for (int index = 0; index < curveCount; index++) bone.Curves.Add(CurveCodec.Read(reader, curves + index * (long)CurveCodec.HeaderSize));
        return bone;
    }

    public static (int Curves, int Base) WriteHeader(SwitchWriter writer, BoneAnim bone)
    {
        writer.StringPointer(bone.Name);
        int curves = bone.Curves.Count > 0 ? writer.Pointer() : writer.Unlisted();
        int baseData = writer.Pointer();
        if (writer.Profile.Modern)
        {
            writer.Pointer((long)bone.Reserved[0]);
            writer.Pointer((long)bone.Reserved[1]);
        }

        writer.Out.U32(bone.Flags).U8(bone.BeginRotate).U8(bone.BeginTranslate).U8((byte)bone.Curves.Count).U8(bone.BeginBaseTranslate)
            .U32((uint)bone.BeginCurve).U32(0);
        return (curves, baseData);
    }

    public static void WriteBase(SwitchWriter writer, BoneAnim bone)
    {
        BoneAnimData data = bone.BaseData!;
        if (bone.FlagsBase.HasFlag(BoneAnimFlagsBase.Scale)) WriteVector(writer, data.Scale);
        if (bone.FlagsBase.HasFlag(BoneAnimFlagsBase.Rotate)) writer.Out.F32(data.Rotate.X).F32(data.Rotate.Y).F32(data.Rotate.Z).F32(data.Rotate.W);
        if (bone.FlagsBase.HasFlag(BoneAnimFlagsBase.Translate)) WriteVector(writer, data.Translate);
    }

    private static BoneAnimData ReadBase(SwitchReader reader, long at, BoneAnimFlagsBase flags)
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

    private static void WriteVector(SwitchWriter writer, Vector3 value) => writer.Out.F32(value.X).F32(value.Y).F32(value.Z);
}
