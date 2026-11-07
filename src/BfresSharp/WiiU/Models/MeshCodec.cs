using BfresSharp.Gx2;
using BfresSharp.Model.Geometry;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes meshes in the Wii U layout, which is 28 bytes.</summary>
/// <remarks>
/// The primitive type, the index format and count, the sub mesh count, a pad, the offsets of the sub mesh list and of
/// the index buffer, and the first vertex. The index buffer is a stream record like those of vertex buffers.
/// </remarks>
public static class MeshCodec
{
    public const int Size = 0x1C;
    private const int IndexAlignment = 4;
    private const int SubMeshSize = 8;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Mesh, object> Keys = new();

    public static Mesh Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        var mesh = new Mesh
        {
            PrimitiveType = (GX2PrimitiveType)view.U32(at),
            Gx2IndexFormat = (GX2IndexFormat)view.U32(at + 4),
            FirstVertex = view.U32(at + 0x18),
            SubMeshes = reader.ReadList(at + 0x10, view.U16(at + 0xC), SubMeshSize, offset => new SubMesh(view.U32(offset), view.U32(offset + 4))),
        };
        mesh.IndexFormat = mesh.Gx2IndexFormat is GX2IndexFormat.UInt32 or GX2IndexFormat.UInt32LittleEndian ? IndexFormat.UInt32 : IndexFormat.UInt16;
        long buffer = reader.Offset(at + 0x14);
        if (buffer == 0) return mesh;
        long data = reader.Offset(buffer + 0x14);
        int copies = Math.Max((int)view.U16(buffer + 0xE), 1);
        mesh.IndexStride = view.U16(buffer + 0xC);
        mesh.IndexBufferCount = view.U16(buffer + 0xE);
        if (data != 0) mesh.IndexData = reader.Bytes(data, (long)view.I32(buffer + 4) * copies);
        return mesh;
    }

    public static void Write(WiiUWriter writer, Mesh mesh, int index)
    {
        var o = writer.Out;
        o.U32((uint)mesh.PrimitiveType).U32((uint)mesh.Gx2IndexFormat).U32((uint)mesh.IndexCount).U16((ushort)mesh.SubMeshes.Count).U16(0);
        writer.Records(mesh.SubMeshes, (sub, _) => o.U32(sub.Offset).U32(sub.Count));
        writer.Item(BufferKey(mesh), -1, (key, _) => WriteBuffer(writer, mesh, key));
        o.U32(mesh.FirstVertex);
    }

    // Places what a mesh refers to: its sub meshes and its index buffer.
    public static void PlaceChildren(WiiUWriter writer, Mesh mesh)
    {
        writer.Place(mesh.SubMeshes, BufferKey(mesh));
    }

    private static object BufferKey(Mesh mesh) => Keys.GetValue(mesh, _ => new object());

    private static void WriteBuffer(WiiUWriter writer, Mesh mesh, object key)
    {
        int copies = Math.Max((int)mesh.IndexBufferCount, 1);
        writer.Out.U32(0).U32((uint)(mesh.IndexData.Length / copies)).U32(0).U16(mesh.IndexStride).U16(mesh.IndexBufferCount).U32(0);
        writer.Block(mesh.IndexData.Length == 0 ? null : mesh.IndexData, new Layout(IndexAlignment, 0));
    }
}
