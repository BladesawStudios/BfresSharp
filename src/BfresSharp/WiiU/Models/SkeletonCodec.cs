using BfresSharp.Binary;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Skeleton;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes skeletons in the Wii U layout, which is 36 bytes from version 3.4.</summary>
/// <remarks>
/// The magic, the flags, the bone, smooth and rigid matrix counts, a pad, the offsets of the bone dictionary and list,
/// the matrix-to-bone table and the inverse model matrices, and a user pointer. Bones are read through the dictionary.
/// </remarks>
public static class SkeletonCodec
{
    private const uint MinVersion = 0x03040000;

    public static Skeleton Read(WiiUReader reader, long at)
    {
        if (!reader.AtLeast(MinVersion)) throw new BfresUnsupportedException("Skeletons before version 3.4 are not supported yet", at);
        var view = reader.View;
        int smooth = view.U16(at + 0xA), matrices = smooth + view.U16(at + 0xC);
        var skeleton = new Skeleton { RawFlags = view.U32(at + 4), Bones = reader.ReadDict(at + 0x10, offset => BoneCodec.Read(reader, offset)) };
        long toBone = reader.Offset(at + 0x18), inverse = reader.Offset(at + 0x1C);
        if (toBone != 0) skeleton.MatrixToBoneList = Enumerable.Range(0, matrices).Select(index => view.U16(toBone + index * 2L)).ToList();
        if (inverse != 0) skeleton.InverseModelMatrices = Enumerable.Range(0, smooth).Select(index => ReadMatrix(view, inverse + index * 48L)).ToList();
        return skeleton;
    }

    public static void Write(WiiUWriter writer, Skeleton skeleton, int index)
    {
        var o = writer.Out;
        o.Bytes("FSKL"u8).U32(skeleton.RawFlags).U16((ushort)skeleton.Bones.Count).U16((ushort)skeleton.NumSmoothMatrices)
            .U16((ushort)skeleton.NumRigidMatrices).U16(0);
        writer.Dict(skeleton.Bones, (bone, at) => BoneCodec.Write(writer, bone, at));
        writer.List(skeleton.Bones.Values, (bone, at) => BoneCodec.Write(writer, bone, at));
        object toBone = writer.Array(() =>
        {
            foreach (ushort bone in skeleton.MatrixToBoneList) o.U16(bone);
        });
        object inverse = writer.Array(() =>
        {
            foreach (Matrix3x4 matrix in skeleton.InverseModelMatrices)
            {
                foreach (float value in matrix.ToArray()) o.F32(value);
            }
        });
        o.U32(0);
        writer.Place(skeleton.Bones.Values, toBone, inverse, skeleton.Bones);
        foreach (Bone bone in skeleton.Bones.Values) writer.PlaceAll(bone.UserData);
    }

    private static Matrix3x4 ReadMatrix(ByteView view, long at)
    {
        var values = new float[Matrix3x4.FloatCount];
        for (int index = 0; index < values.Length; index++) values[index] = view.F32(at + index * 4L);
        return Matrix3x4.FromArray(values);
    }
}
