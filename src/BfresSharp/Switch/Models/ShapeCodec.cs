using System.Numerics;
using BfresSharp.Binary;
using BfresSharp.Model.Geometry;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes shapes in the Switch layout.</summary>
/// <remarks>
/// After the magic come the name, the vertex buffer pointer, the mesh array, skin bone indices, key shape array and
/// dictionary, bounding array and radius pointers and an unused word. Files before version 9 then hold the flags. The
/// bounding array has one box for each mesh and each sub mesh. Version 10 stores a radius vector for each mesh, or for
/// each skin bone when the shape is skinned; earlier files store one radius for each mesh.
/// </remarks>
public static class ShapeCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x60 : 0x70;

    public static Shape Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var profile = reader.Profile;
        long fields = at + (profile.Modern ? 8 : 0x10);
        long counts = fields + 0x48 + (profile.Modern ? 0 : 4);
        int skinCount = view.U16(counts + 8);
        int meshCount = view.U8(counts + 0xB);
        var shape = new Shape
        {
            Name = reader.String(fields) ?? "",
            Flags = view.U32(profile.Modern ? at + 4 : fields + 0x48),
            MaterialIndex = view.U16(counts + 2),
            BoneIndex = view.U16(counts + 4),
            VertexBufferIndex = view.U16(counts + 6),
            VertexSkinCount = view.U8(counts + 0xA),
            TargetAttribCount = view.U8(counts + 0xD),
        };
        long meshes = reader.Pointer(fields + 0x10);
        long skin = reader.Pointer(fields + 0x18);
        shape.KeyShapes = reader.ReadDict(fields + 0x28, fields + 0x20, 24, offset => ReadKeyShape(view, offset));
        if (meshes != 0) shape.Meshes = reader.ReadArray(meshes, meshCount, MeshCodec.Size, offset => MeshCodec.Read(reader, offset)).ToList();
        if (skin != 0) shape.SkinBoneIndices = reader.ReadArray(skin, skinCount, 2, offset => view.U16(offset)).ToList();
        ReadBoundings(reader, shape, fields);
        ReadRadii(reader, shape, reader.Pointer(fields + 0x38), meshCount, skinCount);
        return shape;
    }

    public static Slots WriteHeader(SwitchWriter writer, Shape shape, long vertexBuffer, int index)
    {
        var profile = writer.Profile;
        writer.Out.Bytes("FSHP"u8);
        if (profile.Modern) writer.Out.U32(shape.Flags);
        else writer.Out.Zeros(12);
        writer.StringPointer(shape.Name);
        writer.Pointer(vertexBuffer);
        int meshes = writer.Pointer();
        int skin = writer.Pointer();
        bool keys = shape.KeyShapes.Count > 0;
        int keyArray = keys ? writer.Pointer() : writer.Unlisted();
        int keyDict = keys ? writer.Pointer() : writer.Unlisted();
        var slots = new Slots(meshes, skin, keyArray, keyDict, writer.Pointer(), writer.Pointer());
        writer.Unlisted();
        if (!profile.Modern) writer.Out.U32(shape.Flags);
        writer.Out.U16((ushort)index).U16(shape.MaterialIndex).U16(shape.BoneIndex).U16(shape.VertexBufferIndex)
            .U16((ushort)shape.SkinBoneIndices.Count).U8(shape.VertexSkinCount).U8((byte)shape.Meshes.Count)
            .U8((byte)shape.KeyShapes.Count).U8(shape.TargetAttribCount).Zeros(profile.Modern ? 2 : 6);
        return slots;
    }

    // The mesh headers, skin bone indices, bounding boxes, key shapes, radii and then each mesh's own records.
    public static void WriteBody(SwitchWriter writer, Shape shape, Slots slots, SwitchBufferLayout layout)
    {
        if (shape.Meshes.Count > 0) writer.Align(8).Resolve(slots.Meshes);
        var meshSlots = shape.Meshes.Select(mesh => MeshCodec.WriteHeader(writer, mesh, layout.FaceOffset(mesh))).ToList();
        writer.Align(8).Resolve(slots.SkinBoneIndices);
        foreach (ushort index in shape.SkinBoneIndices) writer.Out.U16(index);

        if (shape.SubMeshBoundings.Count > 0)
        {
            writer.Align(8).Resolve(slots.Boundings);
            foreach (Bounding box in shape.SubMeshBoundings) WriteBounding(writer, box);
        }

        WriteKeyShapes(writer, shape, slots);
        WriteRadii(writer, shape, slots);
        for (int index = 0; index < shape.Meshes.Count; index++)
        {
            MeshCodec.WriteSubMeshes(writer, shape.Meshes[index], meshSlots[index]);
            MeshCodec.WriteBufferRecords(writer, shape.Meshes[index], meshSlots[index]);
        }
    }

    private static void WriteKeyShapes(SwitchWriter writer, Shape shape, Slots slots)
    {
        if (shape.KeyShapes.Count == 0) return;
        writer.Align(8).Resolve(slots.KeyShapes);
        foreach (KeyShape key in shape.KeyShapes.Values) writer.Out.Bytes(key.TargetAttribIndices).Bytes(key.TargetAttribIndexOffsets);
        writer.Align(8).Resolve(slots.KeyShapeDict);
        SwitchDictCodec.Write(writer, shape.KeyShapes);
    }

    private static void WriteRadii(SwitchWriter writer, Shape shape, Slots slots)
    {
        if (shape.Radii.Count == 0 && shape.BoundingRadii.Count == 0) return;
        writer.Align(8).Resolve(slots.Radii);
        foreach (float radius in shape.Radii) writer.Out.F32(radius);
        foreach (Vector4 radius in shape.BoundingRadii) writer.Out.F32(radius.X).F32(radius.Y).F32(radius.Z).F32(radius.W);
    }

    private static void WriteBounding(SwitchWriter writer, Bounding box)
    {
        writer.Out.F32(box.Center.X).F32(box.Center.Y).F32(box.Center.Z).F32(box.Extent.X).F32(box.Extent.Y).F32(box.Extent.Z);
    }

    private static void ReadBoundings(SwitchReader reader, Shape shape, long fields)
    {
        long boundings = reader.Pointer(fields + 0x30);
        int count = shape.Meshes.Sum(mesh => mesh.SubMeshes.Count + 1);
        if (boundings == 0 || count == 0) return;
        var view = reader.View;
        shape.SubMeshBoundings = reader.ReadArray(boundings, count, 24, offset => new Bounding(ReadVector3(view, offset), ReadVector3(view, offset + 12))).ToList();
    }

    private static void ReadRadii(SwitchReader reader, Shape shape, long radii, int meshCount, int skinCount)
    {
        if (radii == 0 || meshCount == 0) return;
        var view = reader.View;
        if (reader.Profile.Version.Major >= 10)
        {
            int count = skinCount == 0 ? meshCount : skinCount;
            shape.BoundingRadii = reader.ReadArray(radii, count, 16, offset => new Vector4(view.F32(offset), view.F32(offset + 4), view.F32(offset + 8), view.F32(offset + 12))).ToList();
        }
        else
        {
            shape.Radii = reader.ReadArray(radii, meshCount, 4, offset => view.F32(offset)).ToList();
        }
    }

    private static KeyShape ReadKeyShape(ByteView view, long at)
    {
        return new KeyShape
        {
            TargetAttribIndices = view.Slice(at, KeyShape.IndexCount).Span.ToArray(),
            TargetAttribIndexOffsets = view.Slice(at + KeyShape.IndexCount, KeyShape.OffsetCount).Span.ToArray(),
        };
    }

    private static Vector3 ReadVector3(ByteView view, long at) => new(view.F32(at), view.F32(at + 4), view.F32(at + 8));

    public readonly record struct Slots(int Meshes, int SkinBoneIndices, int KeyShapes, int KeyShapeDict, int Boundings, int Radii);
}
