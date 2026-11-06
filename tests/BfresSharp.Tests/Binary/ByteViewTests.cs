using BfresSharp.Binary;
using BfresSharp.Errors;

namespace BfresSharp.Tests.Binary;

public sealed class ByteViewTests
{
    private static readonly byte[] Data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09];

    [Fact]
    public void ReadsLittleEndian()
    {
        var view = new ByteView(Data, ByteOrder.Little);

        Assert.Equal(0x0201, view.U16(0));
        Assert.Equal(0x04030201u, view.U32(0));
        Assert.Equal(0x0807060504030201ul, view.U64(0));
    }

    [Fact]
    public void ReadsBigEndian()
    {
        var view = new ByteView(Data, ByteOrder.Big);

        Assert.Equal(0x0102, view.U16(0));
        Assert.Equal(0x01020304u, view.U32(0));
        Assert.Equal(0x0102030405060708ul, view.U64(0));
    }

    [Fact]
    public void ReadsSignedAndFloat()
    {
        var big = new ByteView(new byte[] { 0xFF, 0xFF, 0xFF, 0xFE, 0x3F, 0x80, 0x00, 0x00 }, ByteOrder.Big);

        Assert.Equal(-2, big.I32(0));
        Assert.Equal(1.0f, big.F32(4));
    }

    [Fact]
    public void SliceSharesBytesAndKeepsOrder()
    {
        var view = new ByteView(Data, ByteOrder.Big).Slice(2, 4);

        Assert.Equal(4, view.Length);
        Assert.Equal(0x03040506u, view.U32(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(9)]
    public void ThrowsFormatExceptionWithOffsetWhenReadRunsOut(long offset)
    {
        var view = new ByteView(Data, ByteOrder.Little);

        var error = Assert.Throws<BfresFormatException>(() => view.U32(offset));
        Assert.Equal(offset, error.Offset);
    }

    [Fact]
    public void StartsWithIsFalseOutOfRange()
    {
        var view = new ByteView(Data, ByteOrder.Little);

        Assert.True(view.StartsWith(1, [0x02, 0x03]));
        Assert.False(view.StartsWith(8, [0x09, 0x0A]));
        Assert.False(view.StartsWith(-1, [0x01]));
    }
}
