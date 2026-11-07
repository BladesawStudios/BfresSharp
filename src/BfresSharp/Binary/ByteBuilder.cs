using System.Buffers.Binary;

namespace BfresSharp.Binary;

/// <summary>A growing buffer that values are appended to in one byte order and patched in later.</summary>
/// <remarks>
/// Writing appends at <see cref="Length"/>; the Set methods overwrite bytes already written, which is how a pointer
/// is filled in once its target has a position. <see cref="Align"/> pads with zeros.
/// </remarks>
public sealed class ByteBuilder
{
    private byte[] _buffer;

    public ByteBuilder(ByteOrder order, int capacity = 256)
    {
        Order = order;
        _buffer = new byte[Math.Max(capacity, 16)];
    }

    public ByteOrder Order { get; }

    public int Length { get; private set; }

    public ReadOnlySpan<byte> Written => _buffer.AsSpan(0, Length);

    public ByteBuilder U8(byte value)
    {
        Span<byte> span = Append(1);
        span[0] = value;
        return this;
    }

    public ByteBuilder U16(ushort value)
    {
        Span<byte> span = Append(2);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt16BigEndian(span, value);
        return this;
    }

    public ByteBuilder U32(uint value)
    {
        Span<byte> span = Append(4);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt32BigEndian(span, value);
        return this;
    }

    public ByteBuilder U64(ulong value)
    {
        Span<byte> span = Append(8);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt64BigEndian(span, value);
        return this;
    }

    public ByteBuilder F32(float value) => U32(BitConverter.SingleToUInt32Bits(value));

    public ByteBuilder Bytes(ReadOnlySpan<byte> value)
    {
        value.CopyTo(Append(value.Length));
        return this;
    }

    public ByteBuilder Zeros(int count)
    {
        Append(count).Clear();
        return this;
    }

    public ByteBuilder Align(int alignment)
    {
        int remainder = Length % alignment;
        return remainder == 0 ? this : Zeros(alignment - remainder);
    }

    public void SetU16(int offset, ushort value)
    {
        Span<byte> span = _buffer.AsSpan(offset, 2);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt16BigEndian(span, value);
    }

    public void SetU32(int offset, uint value)
    {
        Span<byte> span = _buffer.AsSpan(offset, 4);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt32BigEndian(span, value);
    }

    public void SetU64(int offset, ulong value)
    {
        Span<byte> span = _buffer.AsSpan(offset, 8);
        if (Order == ByteOrder.Little) BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt64BigEndian(span, value);
    }

    // Overwrites bytes at a position, extending the content with zeros first when it ends before the bytes do.
    public void SetBytes(int offset, ReadOnlySpan<byte> value)
    {
        if (offset + value.Length > Length) Zeros(offset + value.Length - Length);
        value.CopyTo(_buffer.AsSpan(offset, value.Length));
    }

    public byte[] ToArray() => Written.ToArray();

    private Span<byte> Append(int count)
    {
        if (Length + count > _buffer.Length) Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, Length + count));
        Span<byte> span = _buffer.AsSpan(Length, count);
        Length += count;
        return span;
    }
}
