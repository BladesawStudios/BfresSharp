using BfresSharp.Gx2;
using BfresSharp.Model.Geometry;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes vertex buffers in the Switch layout.</summary>
/// <remarks>
/// The fields after the magic are the attribute array and dictionary pointers, the memory pool pointer, two pointers
/// to unused buffer info, the pointers to the stream size and stride records, an unused word, the offset of the first
/// stream inside the buffer data, the attribute and stream counts, the index, the vertex count, the skin count and the
/// GPU alignment. A size record and a stride record are 16 bytes each. Streams are aligned inside the buffer data.
/// </remarks>
public static class VertexBufferCodec
{
    public const int AttribSize = 16;
    private const int RecordSize = 16;
    private const int BufferInfoWords = 9;
    private const int StridePadding = 32;
    private const uint FvtxMagic = 0x58545646;
    private const int BlankIndexAt = 0x4E;

    public static int Size(SwitchProfile profile) => profile.Modern ? 0x58 : 0x60;

    public static VertexBuffer Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        long fields = at + (reader.Profile.Modern ? 8 : 0x10);
        if (view.U32(at) != FvtxMagic) return new VertexBuffer { IsBlank = true };
        var buffer = new VertexBuffer
        {
            Flags = reader.Profile.Modern ? view.U32(at + 4) : 0,
            VertexCount = view.U32(fields + 0x48),
            VertexSkinCount = (byte)view.U16(fields + 0x4C),
            GpuBufferAlignment = reader.Profile.Version.Major >= 10 ? view.U16(fields + 0x4E) : (ushort)8,
            Attributes = reader.ReadDict(fields + 8, fields, AttribSize, offset => ReadAttrib(reader, offset)),
        };
        int streams = view.U8(fields + 0x45);
        long sizes = reader.Pointer(fields + 0x28);
        long strides = reader.Pointer(fields + 0x30);
        long data = reader.BufferBase + view.U32(fields + 0x40);
        for (int index = 0; index < streams; index++)
        {
            data = Align(data, buffer.GpuBufferAlignment);
            long record = sizes + index * (long)RecordSize;
            var stream = new VertexStream
            {
                Stride = (ushort)view.U32(strides + index * (long)RecordSize),
                GpuAccessFlags = view.U32(record + 4),
                Data = view.Slice(data, view.U32(record)).Span.ToArray(),
            };
            data += stream.Data.Length;
            buffer.Streams.Add(stream);
        }

        return buffer;
    }

    public static Slots WriteHeader(SwitchWriter writer, VertexBuffer buffer, int index, uint streamOffset)
    {
        if (buffer.IsBlank)
        {
            // Only the index of the record is filled in.
            writer.Out.Zeros(BlankIndexAt).U16((ushort)index).Zeros(Size(writer.Profile) - BlankIndexAt - 2);
            return default;
        }

        writer.Out.Bytes("FVTX"u8);
        if (writer.Profile.Modern) writer.Out.U32(buffer.Flags);
        else writer.Out.Zeros(12);
        var slots = new Slots(writer.Pointer(), writer.Pointer(), 0, 0, 0, 0);
        writer.PoolPointer();
        int unknown = writer.Pointer();
        int unknown2 = writer.Pointer();
        int size = writer.Pointer();
        int stride = writer.Pointer();
        writer.Out.U64(0).U32(streamOffset).U8((byte)buffer.Attributes.Count).U8((byte)buffer.Streams.Count).U16((ushort)index)
            .U32(buffer.VertexCount).U16(buffer.VertexSkinCount).U16(writer.Profile.Version.Major >= 10 ? buffer.GpuBufferAlignment : (ushort)0);
        return slots with { Unknown = unknown, Unknown2 = unknown2, Sizes = size, Strides = stride };
    }

    // The attribute records, the unused buffer info, the size and stride records, padding and the attribute dictionary.
    public static void WriteBody(SwitchWriter writer, VertexBuffer buffer, Slots slots)
    {
        if (buffer.IsBlank) return;
        writer.Align(8).Resolve(slots.Attributes);
        foreach (VertexAttrib attrib in buffer.Attributes.Values) WriteAttrib(writer, attrib);
        writer.Align(8).Resolve(slots.Unknown);
        writer.Out.Zeros(BufferInfoWords * 8 * buffer.Streams.Count);
        writer.Align(8).Resolve(slots.Sizes);
        foreach (VertexStream stream in buffer.Streams) writer.Out.U32((uint)stream.Data.Length).U32(stream.GpuAccessFlags).Zeros(8);
        writer.Align(8).Resolve(slots.Strides);
        foreach (VertexStream stream in buffer.Streams) writer.Out.U32(stream.Stride).Zeros(12);
        if (!writer.Profile.Modern) writer.Out.Zeros(StridePadding);
        writer.Align(8).Resolve(slots.Unknown2);
        writer.Out.Zeros(8 * buffer.Streams.Count);
        SwitchDictCodec.WriteAt(writer, slots.AttributeDict, buffer.Attributes);
    }

    public static long Align(long value, int alignment) => alignment <= 1 ? value : (value + alignment - 1) / alignment * alignment;

    private static VertexAttrib ReadAttrib(SwitchReader reader, long at)
    {
        var view = reader.View;
        ushort code = (ushort)((view.U8(at + 8) << 8) | view.U8(at + 9));
        return new VertexAttrib
        {
            Name = reader.String(at) ?? "",
            Format = SwitchAttribFormats.Decode(code),
            Offset = view.U16(at + 0xC),
            BufferIndex = view.U16(at + 0xE),
        };
    }

    private static void WriteAttrib(SwitchWriter writer, VertexAttrib attrib)
    {
        writer.StringPointer(attrib.Name);
        ushort code = SwitchAttribFormats.Encode(attrib.Format);
        writer.Out.U8((byte)(code >> 8)).U8((byte)code).U16(0).U16(attrib.Offset).U16(attrib.BufferIndex);
    }

    public readonly record struct Slots(int Attributes, int AttributeDict, int Unknown, int Unknown2, int Sizes, int Strides);
}
