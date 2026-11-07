using BfresSharp.Model.Geometry;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Reads and writes the cluster block of a Switch 2 shape.</summary>
/// <remarks>
/// The block is 80 bytes: the memory pool pointer, then pointers to unused buffer info, the buffer size record, the
/// offset of the data inside the buffer data, and two tables with one 32-bit entry for each cluster level, followed
/// by 32 header bytes whose last word is an empty pointer. The size record is 16 bytes holding the byte size and the GPU access word.
/// </remarks>
public static class ClusterMeshCodec
{
    public const int Size = 0x50;
    private const int BufferInfoWords = 9;
    private const int HeaderAt = 0x30;

    public static ClusterMesh Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var cluster = new ClusterMesh { Header = view.Slice(at + HeaderAt, ClusterMesh.HeaderLength).Span.ToArray() };
        int count = cluster.Header[ClusterMesh.CountIndex];
        long sizeRecord = reader.Pointer(at + 0x10), offset = reader.Pointer(at + 0x18);
        if (sizeRecord != 0)
        {
            cluster.BufferFlags = view.U32(sizeRecord + 4);
            cluster.Data = view.Slice(reader.BufferBase + (long)view.U64(offset), view.U32(sizeRecord)).Span.ToArray();
        }

        long offsets = reader.Pointer(at + 0x20), sizes = reader.Pointer(at + 0x28);
        if (offsets != 0) cluster.Offsets = reader.ReadArray(offsets, count, 4, position => view.U32(position));
        if (sizes != 0) cluster.Sizes = reader.ReadArray(sizes, count, 4, position => view.U32(position));
        return cluster;
    }

    public static Slots WriteHeader(SwitchWriter writer, ClusterMesh cluster)
    {
        writer.PoolPointer();
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer(), writer.Pointer());
        writer.Out.Bytes(cluster.Header.AsSpan(0, ClusterMesh.HeaderLength - 8));
        // The last word is a pointer that the files list although it holds nothing.
        int last = writer.Position;
        writer.Out.U64(0);
        writer.ListPointerAt(last);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, ClusterMesh cluster, Slots slots, uint bufferOffset)
    {
        writer.Align(8).Resolve(slots.Info);
        writer.Out.Zeros(BufferInfoWords * 8);
        writer.Align(8).Resolve(slots.SizeRecord);
        writer.Out.U32((uint)cluster.Data.Length).U32(cluster.BufferFlags).Zeros(8);
        writer.Align(8).Resolve(slots.Offset);
        writer.Out.U64(bufferOffset);
        WriteTable(writer, slots.Offsets, cluster.Offsets);
        WriteTable(writer, slots.Sizes, cluster.Sizes);
    }

    private static void WriteTable(SwitchWriter writer, int slot, uint[] values)
    {
        if (values.Length == 0) return;
        writer.Align(8).Resolve(slot);
        foreach (uint value in values) writer.Out.U32(value);
        writer.Align(8);
    }

    public readonly record struct Slots(int Info, int SizeRecord, int Offset, int Offsets, int Sizes);
}
