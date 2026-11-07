using System.Numerics;
using BfresSharp.Binary;
using BfresSharp.Model;
using BfresSharp.Model.Skeleton;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes bones in the Switch layout.</summary>
/// <remarks>
/// A bone starts with its name and the user data array and dictionary pointers. Version 8 and 9 files then leave
/// 16 bytes unused and later ones 8. The indices, flags and the scale, rotation and position follow.
/// </remarks>
public static class BoneCodec
{
    public static int Size(SwitchProfile profile) => 0x50 + Skip(profile);

    public static Bone Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        long fields = at + 0x18 + Skip(reader.Profile);
        return new Bone
        {
            Name = reader.String(at) ?? "",
            UserData = UserDataCodec.ReadDict(reader, at + 0x10, at + 8),
            ParentIndex = (short)view.U16(fields + 2),
            SmoothMatrixIndex = (short)view.U16(fields + 4),
            RigidMatrixIndex = (short)view.U16(fields + 6),
            BillboardIndex = (short)view.U16(fields + 8),
            RawFlags = view.U32(fields + 0xC),
            Scale = ReadVector3(view, fields + 0x10),
            Rotation = new Vector4(view.F32(fields + 0x1C), view.F32(fields + 0x20), view.F32(fields + 0x24), view.F32(fields + 0x28)),
            Position = ReadVector3(view, fields + 0x2C),
        };
    }

    // Writes the bone headers and returns the user data array and dictionary slots of each.
    public static List<(int Array, int Dict)> WriteHeaders(SwitchWriter writer, ResDict<Bone> bones)
    {
        var slots = new List<(int, int)>();
        for (int index = 0; index < bones.Count; index++)
        {
            Bone bone = bones.Values[index];
            writer.StringPointer(bone.Name);
            slots.Add((writer.Pointer(), writer.Pointer()));
            writer.Out.Zeros(Skip(writer.Profile));
            writer.Out.U16((ushort)index).U16((ushort)bone.ParentIndex).U16((ushort)bone.SmoothMatrixIndex)
                .U16((ushort)bone.RigidMatrixIndex).U16((ushort)bone.BillboardIndex).U16((ushort)bone.UserData.Count).U32(bone.RawFlags);
            WriteVector3(writer, bone.Scale);
            writer.Out.F32(bone.Rotation.X).F32(bone.Rotation.Y).F32(bone.Rotation.Z).F32(bone.Rotation.W);
            WriteVector3(writer, bone.Position);
        }

        return slots;
    }

    private static int Skip(SwitchProfile profile) => profile.Version.Major switch
    {
        > 9 => 8,
        >= 8 => 16,
        _ => 0,
    };

    private static Vector3 ReadVector3(ByteView view, long at) => new(view.F32(at), view.F32(at + 4), view.F32(at + 8));

    private static void WriteVector3(SwitchWriter writer, Vector3 value) => writer.Out.F32(value.X).F32(value.Y).F32(value.Z);
}
