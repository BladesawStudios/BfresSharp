using System.Numerics;
using BfresSharp.Model.Skeleton;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes bones in the Wii U layout, which is 64 bytes.</summary>
/// <remarks>
/// The name, the index, the parent, smooth, rigid and billboard indices, the user data count, the flags, then scale,
/// rotation and position and the offset of the user data dictionary.
/// </remarks>
public static class BoneCodec
{
    public const int Size = 0x40;

    public static Bone Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new Bone
        {
            Name = reader.String(at) ?? "",
            ParentIndex = (short)view.U16(at + 6),
            SmoothMatrixIndex = (short)view.U16(at + 8),
            RigidMatrixIndex = (short)view.U16(at + 0xA),
            BillboardIndex = (short)view.U16(at + 0xC),
            RawFlags = view.U32(at + 0x10),
            Scale = new Vector3(view.F32(at + 0x14), view.F32(at + 0x18), view.F32(at + 0x1C)),
            Rotation = new Vector4(view.F32(at + 0x20), view.F32(at + 0x24), view.F32(at + 0x28), view.F32(at + 0x2C)),
            Position = new Vector3(view.F32(at + 0x30), view.F32(at + 0x34), view.F32(at + 0x38)),
            UserData = UserDataCodec.ReadDict(reader, at + 0x3C),
        };
    }

    public static void Write(WiiUWriter writer, Bone bone, int index)
    {
        var o = writer.Out;
        writer.Name(bone.Name);
        o.U16((ushort)index).U16((ushort)bone.ParentIndex).U16((ushort)bone.SmoothMatrixIndex).U16((ushort)bone.RigidMatrixIndex)
            .U16((ushort)bone.BillboardIndex).U16((ushort)bone.UserData.Count).U32(bone.RawFlags);
        o.F32(bone.Scale.X).F32(bone.Scale.Y).F32(bone.Scale.Z);
        o.F32(bone.Rotation.X).F32(bone.Rotation.Y).F32(bone.Rotation.Z).F32(bone.Rotation.W);
        o.F32(bone.Position.X).F32(bone.Position.Y).F32(bone.Position.Z);
        UserDataCodec.WriteDict(writer, bone.UserData);
    }
}
