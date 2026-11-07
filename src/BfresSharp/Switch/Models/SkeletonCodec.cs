using BfresSharp.Binary;
using BfresSharp.Model;
using BfresSharp.Model.Skeleton;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes skeletons in the Switch layout.</summary>
/// <remarks>
/// After the magic come the pointers to the bone dictionary and array, the matrix to bone list and the inverse model
/// matrices, then an unused pointer. Version 8 leaves 16 more bytes unused; version 9 and later add a pointer to the
/// mirrored bone indices. The three counts follow the flags, which version 9 and later keep right after the magic.
/// </remarks>
public static class SkeletonCodec
{
    public static Skeleton Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var profile = reader.Profile;
        long fields = at + (profile.Modern ? 8 : 0x10);
        long counts = fields + CountsOffset(profile);
        int boneCount = view.U16(counts);
        int smooth = view.U16(counts + 2);
        int rigid = view.U16(counts + 4);
        var skeleton = new Skeleton
        {
            RawFlags = profile.Modern ? view.U32(at + 4) : view.U32(counts - 4),
            Bones = reader.ReadDict(fields, fields + 8, BoneCodec.Size(profile), offset => BoneCodec.Read(reader, offset)),
        };
        long matrixToBone = reader.Pointer(fields + 0x10);
        long inverse = reader.Pointer(fields + 0x18);
        if (matrixToBone != 0) skeleton.MatrixToBoneList = reader.ReadArray(matrixToBone, smooth + rigid, 2, offset => view.U16(offset)).ToList();
        if (inverse != 0) skeleton.InverseModelMatrices = reader.ReadArray(inverse, smooth, 48, offset => ReadMatrix(view, offset)).ToList();
        long mirrored = profile.Modern ? reader.Pointer(fields + 0x28) : 0;
        if (mirrored != 0) skeleton.MirroredBoneIndices = reader.ReadArray(mirrored, boneCount, 2, offset => view.U16(offset));
        return skeleton;
    }

    public static Slots WriteHeader(SwitchWriter writer, Skeleton skeleton)
    {
        var profile = writer.Profile;
        writer.Out.Bytes("FSKL"u8);
        if (profile.Modern) writer.Out.U32(skeleton.RawFlags);
        else writer.Block();
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), 0);
        writer.Unlisted();
        if (profile.Version.Major == 8) writer.Out.Zeros(16);
        int mirrored = profile.Modern ? writer.Pointer() : 0;
        if (!profile.Modern) writer.Out.U32(skeleton.RawFlags);
        writer.Out.U16((ushort)skeleton.Bones.Count).U16((ushort)skeleton.NumSmoothMatrices).U16((ushort)skeleton.NumRigidMatrices);
        writer.Out.Zeros(profile.Modern ? 2 : 6);
        return slots with { Mirrored = mirrored };
    }

    // The bones, then the matrix lists, the mirror table, the bone dictionary and the user data of every bone.
    public static void WriteBody(SwitchWriter writer, Skeleton skeleton, Slots slots)
    {
        if (skeleton.Bones.Count > 0) writer.Align(8).Resolve(slots.BoneArray);
        var boneSlots = BoneCodec.WriteHeaders(writer, skeleton.Bones);
        WriteMatrixLists(writer, skeleton, slots);
        if (slots.Mirrored != 0)
        {
            writer.Resolve(slots.Mirrored);
            foreach (ushort index in skeleton.MirroredBoneIndices) writer.Out.U16(index);
            writer.Align(8);
        }

        if (skeleton.Bones.Count > 0)
        {
            writer.Align(8).Resolve(slots.BoneDict);
            SwitchDictCodec.Write(writer, skeleton.Bones);
        }

        WriteBoneUserData(writer, skeleton, boneSlots);
    }

    // Both lists are pointed at even when empty, as the files do.
    private static void WriteMatrixLists(SwitchWriter writer, Skeleton skeleton, Slots slots)
    {
        writer.Align(8).Resolve(slots.MatrixToBone);
        foreach (ushort index in skeleton.MatrixToBoneList) writer.Out.U16(index);
        writer.Align(8).Resolve(slots.InverseMatrices);
        foreach (Matrix3x4 matrix in skeleton.InverseModelMatrices)
        {
            foreach (float value in matrix.ToArray()) writer.Out.F32(value);
        }

        writer.Align(8);
    }

    private static void WriteBoneUserData(SwitchWriter writer, Skeleton skeleton, List<(int Array, int Dict)> boneSlots)
    {
        for (int index = 0; index < skeleton.Bones.Count; index++)
        {
            var userData = skeleton.Bones.Values[index].UserData;
            if (userData.Count == 0) continue;
            int[] valueSlots = UserDataCodec.WriteArray(writer, userData, boneSlots[index].Array);
            UserDataCodec.WriteDictAndValues(writer, userData, boneSlots[index].Dict, valueSlots);
        }
    }

    private static int CountsOffset(SwitchProfile profile) => profile.Version.Major switch
    {
        >= 9 => 0x30,
        8 => 0x3C,
        _ => 0x2C,
    };

    private static Matrix3x4 ReadMatrix(ByteView view, long at)
    {
        var values = new float[Matrix3x4.FloatCount];
        for (int index = 0; index < values.Length; index++) values[index] = view.F32(at + index * 4L);
        return Matrix3x4.FromArray(values);
    }

    public readonly record struct Slots(int BoneDict, int BoneArray, int MatrixToBone, int InverseMatrices, int Mirrored);
}
