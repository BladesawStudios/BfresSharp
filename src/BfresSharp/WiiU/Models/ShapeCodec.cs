using System.Numerics;
using BfresSharp.Binary;
using BfresSharp.Model;
using BfresSharp.Model.Geometry;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes shapes in the Wii U layout.</summary>
/// <remarks>
/// A shape (FSHP) holds its name, flags, the indices of its material, bone and vertex buffer, the counts of its skin
/// bones, meshes, key shapes and bounding nodes, then offsets to the radii (a single value before version 4.5), the vertex
/// buffer, the mesh list, the skin bone indices, the key shape dictionary and the bounding data. A shape without
/// bounding nodes keeps only the bounding boxes, which makes its record 8 bytes shorter.
/// </remarks>
public static class ShapeCodec
{
    private const uint RadiiArrayVersion = 0x04050000;
    private const int BoundingSize = 24;
    private const int NodeSize = 12;
    private const int KeyShapeSize = 24;

    public static Shape Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        int meshCount = view.U8(at + 0x17), nodeCount = view.U16(at + 0x1A);
        var shape = new Shape
        {
            Name = reader.String(at + 4) ?? "",
            Flags = view.U32(at + 8),
            MaterialIndex = view.U16(at + 0xE),
            BoneIndex = view.U16(at + 0x10),
            VertexBufferIndex = view.U16(at + 0x12),
            VertexSkinCount = view.U8(at + 0x16),
            TargetAttribCount = view.U8(at + 0x19),
            Meshes = reader.ReadList(at + 0x24, meshCount, MeshCodec.Size, offset => MeshCodec.Read(reader, offset)),
            KeyShapes = reader.ReadDict(at + 0x2C, offset => ReadKeyShape(view, offset)),
        };
        long skin = reader.Offset(at + 0x28);
        if (skin != 0) shape.SkinBoneIndices = Enumerable.Range(0, view.U16(at + 0x14)).Select(index => view.U16(skin + index * 2L)).ToList();
        ReadRadii(reader, shape, at, meshCount);
        ReadBounds(reader, shape, at, nodeCount);
        return shape;
    }

    public static void Write(WiiUWriter writer, Shape shape, VertexBuffer? buffer, int index)
    {
        var o = writer.Out;
        o.Bytes("FSHP"u8);
        writer.Name(shape.Name);
        o.U32(shape.Flags).U16((ushort)index).U16(shape.MaterialIndex).U16(shape.BoneIndex).U16(shape.VertexBufferIndex)
            .U16((ushort)shape.SkinBoneIndices.Count).U8(shape.VertexSkinCount).U8((byte)shape.Meshes.Count).U8((byte)shape.KeyShapes.Count)
            .U8(shape.TargetAttribCount).U16((ushort)shape.SubMeshBoundingNodes.Count);
        if (writer.AtLeast(RadiiArrayVersion)) writer.Custom(shape.Radii.Count == 0 ? null : shape.Radii, () => WriteFloats(o, shape.Radii));
        else o.F32(shape.Radii.Count > 0 ? shape.Radii[0] : 0);
        writer.Item(buffer, -1, (vertices, at) => VertexBufferCodec.Write(writer, vertices, at));
        writer.List(shape.Meshes, (mesh, at) => MeshCodec.Write(writer, mesh, at));
        object skin = writer.Array(() => WriteShorts(o, shape.SkinBoneIndices));
        writer.Dict(shape.KeyShapes, (key, _) => WriteKeyShape(o, key));
        WriteBounds(writer, shape);
        o.U32(0);
        writer.Place(shape.Meshes, shape.KeyShapes);
        writer.PlaceValues(shape.KeyShapes);
        writer.Place(skin, shape.SubMeshBoundingNodes, shape.SubMeshBoundings, shape.SubMeshBoundingIndices, shape.Radii);
        foreach (Mesh mesh in shape.Meshes) MeshCodec.PlaceChildren(writer, mesh);
    }

    private static void ReadRadii(WiiUReader reader, Shape shape, long at, int meshCount)
    {
        if (!reader.AtLeast(RadiiArrayVersion))
        {
            shape.Radii = [reader.View.F32(at + 0x1C)];
            return;
        }

        long radii = reader.Offset(at + 0x1C);
        if (radii != 0) shape.Radii = Enumerable.Range(0, meshCount).Select(index => reader.View.F32(radii + index * 4L)).ToList();
    }

    private static void ReadBounds(WiiUReader reader, Shape shape, long at, int nodeCount)
    {
        var view = reader.View;
        if (nodeCount == 0)
        {
            int count = reader.AtLeast(RadiiArrayVersion) ? shape.Meshes.Count + shape.Meshes.Sum(mesh => mesh.SubMeshes.Count) : 2 + shape.Meshes[0].SubMeshes.Count;
            shape.SubMeshBoundings = ReadBoundings(view, reader.Offset(at + 0x30), count);
            return;
        }

        shape.SubMeshBoundingNodes = reader.ReadList(at + 0x30, nodeCount, NodeSize, offset => ReadNode(view, offset));
        shape.SubMeshBoundings = ReadBoundings(view, reader.Offset(at + 0x34), nodeCount);
        long indices = reader.Offset(at + 0x38);
        if (indices != 0) shape.SubMeshBoundingIndices = Enumerable.Range(0, nodeCount).Select(index => view.U16(indices + index * 2L)).ToList();
    }

    private static void WriteBounds(WiiUWriter writer, Shape shape)
    {
        var o = writer.Out;
        if (shape.SubMeshBoundingNodes.Count > 0) writer.Records(shape.SubMeshBoundingNodes, (node, _) => WriteNode(o, node));
        writer.Custom(shape.SubMeshBoundings.Count == 0 ? null : shape.SubMeshBoundings, () => WriteBoundings(o, shape.SubMeshBoundings));
        if (shape.SubMeshBoundingNodes.Count > 0) writer.Custom(shape.SubMeshBoundingIndices.Count == 0 ? null : shape.SubMeshBoundingIndices, () => WriteShorts(o, shape.SubMeshBoundingIndices));
    }

    private static List<Bounding> ReadBoundings(ByteView view, long at, int count)
    {
        var result = new List<Bounding>(count);
        if (at == 0) return result;
        for (int index = 0; index < count; index++)
        {
            long item = at + index * (long)BoundingSize;
            result.Add(new Bounding(new Vector3(view.F32(item), view.F32(item + 4), view.F32(item + 8)), new Vector3(view.F32(item + 12), view.F32(item + 16), view.F32(item + 20))));
        }

        return result;
    }

    private static void WriteBoundings(ByteBuilder o, List<Bounding> boundings)
    {
        foreach (Bounding box in boundings)
        {
            o.F32(box.Center.X).F32(box.Center.Y).F32(box.Center.Z).F32(box.Extent.X).F32(box.Extent.Y).F32(box.Extent.Z);
        }
    }

    private static BoundingNode ReadNode(ByteView view, long at)
    {
        return new BoundingNode
        {
            LeftChildIndex = view.U16(at),
            RightChildIndex = view.U16(at + 2),
            Unknown = view.U16(at + 4),
            NextSibling = view.U16(at + 6),
            SubMeshIndex = view.U16(at + 8),
            SubMeshCount = view.U16(at + 10),
        };
    }

    private static void WriteNode(ByteBuilder o, BoundingNode node)
    {
        o.U16(node.LeftChildIndex).U16(node.RightChildIndex).U16(node.Unknown).U16(node.NextSibling).U16(node.SubMeshIndex).U16(node.SubMeshCount);
    }

    private static KeyShape ReadKeyShape(ByteView view, long at)
    {
        return new KeyShape
        {
            TargetAttribIndices = view.Slice(at, KeyShape.IndexCount).Span.ToArray(),
            TargetAttribIndexOffsets = view.Slice(at + KeyShape.IndexCount, KeyShape.OffsetCount).Span.ToArray(),
        };
    }

    private static void WriteKeyShape(ByteBuilder o, KeyShape key)
    {
        o.Bytes(key.TargetAttribIndices).Bytes(key.TargetAttribIndexOffsets);
    }

    private static void WriteFloats(ByteBuilder o, List<float> values)
    {
        foreach (float value in values) o.F32(value);
    }

    private static void WriteShorts(ByteBuilder o, List<ushort> values)
    {
        foreach (ushort value in values) o.U16(value);
    }
}
