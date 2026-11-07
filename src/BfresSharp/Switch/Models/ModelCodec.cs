using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Geometry;
using BfresSharp.Model.Material;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes models in the Switch layout.</summary>
/// <remarks>
/// After the magic come the name, the path, the skeleton, the vertex buffer array, the shape array and dictionary, the
/// material array and dictionary and the user data array and dictionary. Version 9 files pad the material pointers and
/// version 10 adds a shader assign array. The vertex buffer, shape, material and user data counts come last.
/// </remarks>
public static class ModelCodec
{
    public static int Size(SwitchProfile profile) => profile.Modern ? 0x68 : 0x78;

    public static ResModel Read(SwitchReader reader, long at)
    {
        // TODO: Versions 9 and later have a different pointer layout and the shader assign array.
        if (reader.Profile.Version.Major >= 9) throw new BfresFormatException("Reading models of version 9 and later is not supported yet", at);
        var view = reader.View;
        long fields = at + 0x10;
        int buffers = view.U16(fields + 0x58);
        var model = new ResModel
        {
            Name = reader.String(fields) ?? "",
            Path = reader.String(fields + 8) ?? "",
            Skeleton = SkeletonCodec.Read(reader, reader.Pointer(fields + 0x10)),
            Shapes = reader.ReadDict(fields + 0x28, fields + 0x20, ShapeCodec.Size(reader.Profile), offset => ShapeCodec.Read(reader, offset)),
            Materials = reader.ReadDict(fields + 0x38, fields + 0x30, MaterialCodec.Size(reader.Profile), offset => MaterialCodec.Read(reader, offset)),
            UserData = UserDataCodec.ReadDict(reader, fields + 0x48, fields + 0x40),
        };
        long vertexBuffers = reader.Pointer(fields + 0x18);
        if (vertexBuffers != 0) model.VertexBuffers = reader.ReadArray(vertexBuffers, buffers, VertexBufferCodec.Size(reader.Profile), offset => VertexBufferCodec.Read(reader, offset)).ToList();
        return model;
    }
}
