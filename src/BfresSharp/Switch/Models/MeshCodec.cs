using BfresSharp.Gx2;
using BfresSharp.Model.Geometry;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes meshes in the Switch layout.</summary>
/// <remarks>
/// A mesh is 56 bytes: the sub mesh array pointer, the memory pool pointer, a pointer to unused buffer info, a pointer
/// to the buffer size record, then the offset of its indices inside the buffer data, the primitive type, the index
/// format, the index count, the first vertex and the sub mesh count. The size record is 16 bytes holding the byte
/// size and the GPU access word. Face data comes first in the buffer data, each mesh padded to 8 bytes.
/// </remarks>
public static class MeshCodec
{
    public const int Size = 0x38;
    public const int SizeRecordLength = 16;
    private const int BufferInfoWords = 9;
    private const int SizeRecordPadding = 32;

    private static readonly GX2PrimitiveType[] Primitives =
    [
        GX2PrimitiveType.Points, GX2PrimitiveType.Lines, GX2PrimitiveType.LineStrip, GX2PrimitiveType.Triangles,
        GX2PrimitiveType.TriangleStrip, GX2PrimitiveType.LinesAdjacency, GX2PrimitiveType.LineStripAdjacency,
        GX2PrimitiveType.TrianglesAdjacency, GX2PrimitiveType.TriangleStripAdjacency,
    ];

    public static Mesh Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        long subMeshes = reader.Pointer(at);
        long sizeRecord = reader.Pointer(at + 0x18);
        uint primitive = view.U32(at + 0x24);
        var mesh = new Mesh
        {
            PrimitiveType = primitive < Primitives.Length ? Primitives[primitive] : GX2PrimitiveType.Triangles,
            IndexFormat = (IndexFormat)view.U32(at + 0x28),
            FirstVertex = view.U32(at + 0x30),
        };
        int subMeshCount = view.U16(at + 0x34);
        if (subMeshes != 0) mesh.SubMeshes = reader.ReadArray(subMeshes, subMeshCount, 8, offset => new SubMesh(view.U32(offset), view.U32(offset + 4))).ToList();
        if (sizeRecord != 0)
        {
            mesh.BufferFlags = view.U32(sizeRecord + 4);
            int length = (int)view.U32(sizeRecord);
            mesh.IndexData = view.Slice(reader.BufferBase + view.U32(at + 0x20), length).Span.ToArray();
        }

        return mesh;
    }

    // Writes the mesh header; the sub mesh and size record slots are returned for the body.
    public static Slots WriteHeader(SwitchWriter writer, Mesh mesh, uint faceOffset)
    {
        var slots = new Slots(writer.Pointer(), 0, 0);
        writer.PoolPointer();
        int unknown = writer.Pointer();
        int size = writer.Pointer();
        writer.Out.U32(faceOffset).U32((uint)Array.IndexOf(Primitives, mesh.PrimitiveType)).U32((uint)mesh.IndexFormat)
            .U32((uint)mesh.IndexCount).U32(mesh.FirstVertex).U16((ushort)mesh.SubMeshes.Count).U16(0);
        return slots with { Unknown = unknown, SizeRecord = size };
    }

    public static void WriteSubMeshes(SwitchWriter writer, Mesh mesh, Slots slots)
    {
        if (mesh.SubMeshes.Count == 0) return;
        writer.Align(8).Resolve(slots.SubMeshes);
        foreach (SubMesh subMesh in mesh.SubMeshes) writer.Out.U32(subMesh.Offset).U32(subMesh.Count);
    }

    public static void WriteBufferRecords(SwitchWriter writer, Mesh mesh, Slots slots)
    {
        writer.Align(8).Resolve(slots.Unknown);
        writer.Out.Zeros(BufferInfoWords * 8);
        writer.Resolve(slots.SizeRecord);
        writer.Out.U32((uint)mesh.IndexData.Length).U32(mesh.BufferFlags).Zeros(8 + (writer.Profile.Modern ? 0 : SizeRecordPadding));
    }

    public readonly record struct Slots(int SubMeshes, int Unknown, int SizeRecord);
}
