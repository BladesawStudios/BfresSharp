using System.Buffers.Binary;
using BfresSharp.Errors;

namespace BfresSharp.Binary;

/// <summary>A read-only window over file bytes that reads fixed-size values at absolute offsets in one byte order.</summary>
/// <remarks>
/// Holds no position, so any number of readers can use one view at once and nothing is copied: slices share the
/// bytes they were cut from. Every read checks its range and throws <see cref="BfresFormatException"/> with the
/// offending offset, never an index error from the runtime.
/// </remarks>
public readonly struct ByteView
{
    private readonly ReadOnlyMemory<byte> _data;

    public ByteView(ReadOnlyMemory<byte> data, ByteOrder order)
    {
        _data = data;
        Order = order;
    }

    public ByteOrder Order { get; }

    public int Length => _data.Length;

    public ReadOnlySpan<byte> Span => _data.Span;

    public ReadOnlyMemory<byte> Memory => _data;

    public ByteView WithOrder(ByteOrder order) => new(_data, order);

    public ByteView Slice(long offset, long length)
    {
        Check(offset, length);
        return new ByteView(_data.Slice((int)offset, (int)length), Order);
    }

    public byte U8(long offset) => Read(offset, 1)[0];

    public ushort U16(long offset)
    {
        ReadOnlySpan<byte> bytes = Read(offset, 2);
        return Order == ByteOrder.Little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    public uint U32(long offset)
    {
        ReadOnlySpan<byte> bytes = Read(offset, 4);
        return Order == ByteOrder.Little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    public ulong U64(long offset)
    {
        ReadOnlySpan<byte> bytes = Read(offset, 8);
        return Order == ByteOrder.Little ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) : BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }

    public int I32(long offset) => unchecked((int)U32(offset));

    public float F32(long offset) => BitConverter.UInt32BitsToSingle(U32(offset));

    public bool StartsWith(long offset, ReadOnlySpan<byte> expected)
    {
        return offset >= 0 && offset + expected.Length <= Length && Span.Slice((int)offset, expected.Length).SequenceEqual(expected);
    }

    private ReadOnlySpan<byte> Read(long offset, int size)
    {
        Check(offset, size);
        return _data.Span.Slice((int)offset, size);
    }

    private void Check(long offset, long size)
    {
        if (offset < 0 || size < 0 || offset + size > Length)
            throw new BfresFormatException($"Read of {size} bytes runs past the end of the {Length}-byte data", offset);
    }
}
