using BfresSharp.Binary;
using BfresSharp.Container;
using BfresSharp.Errors;

namespace BfresSharp.Tests.Container;

public sealed class BfresHeaderReaderTests
{
    // The first 32 bytes of real files: a Switch 10.0.0 animation file and a Switch 5.0.3 model.
    private const string Switch10 = "4652455320202020 00000a00 fffe0300 2ad00300 0000e4cf 00d20300 a0d30300";
    private const string Switch5 = "4652455320202020 03000500 fffe0c00 a6e60500 0000d000 00130600 c02c0600";

    // A Wii U 3.5.0.3 header: version, byte order mark, header size, file size, alignment.
    private const string WiiU3 = "46524553 03050003 feff0010 00063988 00002000";

    [Fact]
    public void ReadsRealSwitchVersion10Header()
    {
        BfresHeader header = BfresHeaderReader.Read(Bytes(Switch10));

        Assert.Equal(BfresPlatform.Switch, header.Platform);
        Assert.Equal(ByteOrder.Little, header.ByteOrder);
        Assert.Equal("10.0.0", header.Version.ToString());
        Assert.Equal(250784u, header.FileSize);
        Assert.Equal(8u, header.DataAlignment);
    }

    [Fact]
    public void ReadsRealSwitchVersion5Header()
    {
        BfresHeader header = BfresHeaderReader.Read(Bytes(Switch5));

        Assert.Equal("5.0.3", header.Version.ToString());
        Assert.Equal(404672u, header.FileSize);
        Assert.Equal(4096u, header.DataAlignment);
    }

    [Fact]
    public void ReadsWiiUHeader()
    {
        BfresHeader header = BfresHeaderReader.Read(Bytes(WiiU3));

        Assert.Equal(BfresPlatform.WiiU, header.Platform);
        Assert.Equal(ByteOrder.Big, header.ByteOrder);
        Assert.Equal("3.5.0.3", header.Version.ToString());
        Assert.Equal(0x63988u, header.FileSize);
        Assert.Equal(0x2000u, header.DataAlignment);
    }

    [Fact]
    public void VersionKeepsTheWordAsStored()
    {
        Assert.Equal(0x000A0000u, BfresHeaderReader.Read(Bytes(Switch10)).Version.Raw);
        Assert.Equal(0x03050003u, BfresHeaderReader.Read(Bytes(WiiU3)).Version.Raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("46524553")]
    [InlineData("5961 7a30 00000000 00000000 00000000 00000000")]
    [InlineData("4652455320202020 00000a00 0000 0300 2ad00300 0000e4cf 00d20300 a0d30300")]
    [InlineData("4652455320202020 00000a00 fffe2000 2ad00300 0000e4cf 00d20300 a0d30300")]
    [InlineData("46524553 03050003 0000 0010 00063988 00002000")]
    public void RejectsBadHeadersWithoutThrowingFromTryRead(string hex)
    {
        Assert.False(BfresHeaderReader.TryRead(Bytes(hex), out _));
        Assert.Throws<BfresFormatException>(() => BfresHeaderReader.Read(Bytes(hex)));
    }

    private static byte[] Bytes(string hex) => Convert.FromHexString(hex.Replace(" ", ""));
}
