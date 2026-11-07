using BfresSharp.Gx2;
using BfresSharp.Model.Geometry;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes vertex buffers in the Wii U layout.</summary>
/// <remarks>
/// A buffer (FVTX) is 32 bytes: the magic, the attribute and stream counts, the index, the vertex count, the skin count and
/// offsets to the attribute list, the attribute dictionary and the stream list. An attribute is 12 bytes. A stream is 24
/// bytes: a runtime pointer, the size, a handle, the stride, the number of copies, a context pointer and the offset of
/// the data, which is written behind the name pool at 64-byte alignment.
/// </remarks>
public static class VertexBufferCodec
{
    public const int DataAlignment = 0x40;
    public const int DataRank = 1;
    private const int StreamSize = 0x18;

    public static VertexBuffer Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new VertexBuffer
        {
            VertexCount = view.U32(at + 8),
            VertexSkinCount = view.U8(at + 0xC),
            Attributes = reader.ReadDict(at + 0x14, offset => ReadAttrib(reader, offset)),
            Streams = reader.ReadList(at + 0x18, view.U8(at + 5), StreamSize, offset => ReadStream(reader, offset)),
        };
    }

    public static void Write(WiiUWriter writer, VertexBuffer buffer, int index)
    {
        writer.Out.Bytes("FVTX"u8).U8((byte)buffer.Attributes.Count).U8((byte)buffer.Streams.Count).U16((ushort)index)
            .U32(buffer.VertexCount).U8(buffer.VertexSkinCount).Zeros(3);
        writer.List(buffer.Attributes.Values, (attrib, at) => WriteAttrib(writer, attrib));
        writer.Dict(buffer.Attributes, (attrib, at) => WriteAttrib(writer, attrib));
        writer.List(buffer.Streams, (stream, at) => WriteStream(writer, stream));
        writer.Out.U32(0);
    }

    // Places what a vertex buffer refers to: the attributes, the streams and the attribute dictionary.
    public static void PlaceChildren(WiiUWriter writer, VertexBuffer buffer)
    {
        writer.Place(buffer.Attributes.Values, buffer.Streams, buffer.Attributes);
    }

    private static VertexAttrib ReadAttrib(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new VertexAttrib { Name = reader.String(at) ?? "", BufferIndex = view.U8(at + 4), Offset = view.U16(at + 6), Format = (GX2AttribFormat)view.U32(at + 8) };
    }

    private static void WriteAttrib(WiiUWriter writer, VertexAttrib attrib)
    {
        writer.Name(attrib.Name);
        writer.Out.U8((byte)attrib.BufferIndex).U8(0).U16(attrib.Offset).U32((uint)attrib.Format);
    }

    private static VertexStream ReadStream(WiiUReader reader, long at)
    {
        var view = reader.View;
        int size = view.I32(at + 4), copies = Math.Max((int)view.U16(at + 0xE), 1);
        long data = reader.Offset(at + 0x14);
        return new VertexStream
        {
            Stride = view.U16(at + 0xC),
            BufferCount = view.U16(at + 0xE),
            Data = data == 0 ? [] : reader.Bytes(data, (long)size * copies),
        };
    }

    private static void WriteStream(WiiUWriter writer, VertexStream stream)
    {
        int copies = Math.Max((int)stream.BufferCount, 1);
        writer.Out.U32(0).U32((uint)(stream.Data.Length / copies)).U32(0).U16(stream.Stride).U16(stream.BufferCount).U32(0);
        writer.Block(stream.Data.Length == 0 ? null : stream.Data, new Layout(DataAlignment, DataRank));
    }
}
