using BfresSharp.Model;
using BfresSharp.Model.Geometry;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes models in the Wii U layout, which is 48 bytes from version 3.3.</summary>
/// <remarks>
/// The magic, name, path, then offsets to the skeleton, the vertex buffer list and the shape, material and user data
/// dictionaries, the four counts, the total vertex count and a user pointer. The vertex buffers, shapes and materials
/// are queued behind the model in that order.
/// </remarks>
public static class ModelCodec
{
    private const int VertexBufferSize = 0x20;

    public static ResModel Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        var model = new ResModel
        {
            Name = reader.String(at + 4) ?? "",
            Path = reader.String(at + 8) ?? "",
            TotalVertexCount = view.U32(at + 0x28),
            Skeleton = reader.ReadOne(at + 0xC, offset => SkeletonCodec.Read(reader, offset)) ?? new(),
            VertexBuffers = reader.ReadList(at + 0x10, view.U16(at + 0x20), VertexBufferSize, offset => VertexBufferCodec.Read(reader, offset)),
            Shapes = reader.ReadDict(at + 0x14, offset => ShapeCodec.Read(reader, offset)),
            Materials = reader.ReadDict(at + 0x18, offset => MaterialCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, at + 0x1C),
        };
        return model;
    }

    public static void Write(WiiUWriter writer, ResModel model, ResDict<Model.Texture.Texture> textures)
    {
        var o = writer.Out;
        o.Bytes("FMDL"u8);
        writer.Name(model.Name);
        writer.Name(model.Path);
        writer.Item(model.Skeleton, -1, (skeleton, at) => SkeletonCodec.Write(writer, skeleton, at));
        writer.List(model.VertexBuffers, (buffer, at) => VertexBufferCodec.Write(writer, buffer, at), pointWhenEmpty: true);
        writer.Dict(model.Shapes, (shape, at) => ShapeCodec.Write(writer, shape, BufferOf(model, shape), at));
        writer.Dict(model.Materials, (material, at) => MaterialCodec.Write(writer, material, textures, at));
        UserDataCodec.WriteDict(writer, model.UserData);
        o.U16((ushort)model.VertexBuffers.Count).U16((ushort)model.Shapes.Count).U16((ushort)model.Materials.Count).U16((ushort)model.UserData.Count)
            .U32(model.TotalVertexCount).U32(0);
        writer.Place(model.VertexBuffers, model.Skeleton, model.Shapes, model.Materials, model.UserData);
        writer.PlaceValues(model.Shapes);
        foreach (VertexBuffer buffer in model.VertexBuffers) VertexBufferCodec.PlaceChildren(writer, buffer);
        writer.PlaceValues(model.Materials);
        writer.PlaceValues(model.UserData);
    }

    private static VertexBuffer? BufferOf(ResModel model, Shape shape)
    {
        return shape.VertexBufferIndex < model.VertexBuffers.Count ? model.VertexBuffers[shape.VertexBufferIndex] : null;
    }
}
