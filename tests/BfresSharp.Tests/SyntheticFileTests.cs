using BfresSharp.Container;
using BfresSharp.Errors;
using BfresSharp.Model;

namespace BfresSharp.Tests;

/// <summary>Writes files built in code and reads them back, so the library is tested without any game files.</summary>
public sealed class SyntheticFileTests
{
    public static TheoryData<BfresPlatform, uint> Versions => new()
    {
        { BfresPlatform.WiiU, 0x03040003 },
        { BfresPlatform.Switch, 0x00050003 },
        { BfresPlatform.Switch, 0x00090000 },
        { BfresPlatform.Switch2, 0x000A0400 },
    };

    [Theory]
    [MemberData(nameof(Versions))]
    public void EmbeddedFilesSurviveWritingAndReading(BfresPlatform platform, uint raw)
    {
        ResFile file = Build(platform, raw);

        byte[] written = BfresFile.Write(file);
        ResFile read = BfresFile.Read(written);

        Assert.Equal(file.Name, read.Name);
        Assert.Equal(platform, read.Platform);
        Assert.Equal(file.ExternalFiles.Keys, read.ExternalFiles.Keys);
        for (int index = 0; index < file.ExternalFiles.Count; index++) Assert.Equal(file.ExternalFiles.Values[index].Data.ToArray(), read.ExternalFiles.Values[index].Data.ToArray());
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void LargeDataStaysASliceOfTheBytesItWasReadFrom(BfresPlatform platform, uint raw)
    {
        byte[] written = BfresFile.Write(Build(platform, raw));
        ResFile read = BfresFile.Read(written);

        ReadOnlyMemory<byte> data = read.ExternalFiles.Values[0].Data;
        Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment));
        Assert.Same(written, segment.Array);
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void WritingWhatWasReadGivesTheSameBytes(BfresPlatform platform, uint raw)
    {
        byte[] first = BfresFile.Write(Build(platform, raw));
        byte[] second = BfresFile.Write(BfresFile.Read(first));

        Assert.True(first.AsSpan().SequenceEqual(second));
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void StreamAndArrayWritersAgree(BfresPlatform platform, uint raw)
    {
        ResFile file = Build(platform, raw);
        using var stream = new MemoryStream();

        BfresFile.Write(file, stream);

        Assert.True(BfresFile.Write(file).AsSpan().SequenceEqual(stream.ToArray()));
    }

    [Fact]
    public void BytesThatAreNotAFileAreRejectedAsBadFormat()
    {
        Assert.Throws<BfresFormatException>(() => BfresFile.Read(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Fact]
    public void UnsupportedIsAKindOfFormatError()
    {
        Assert.True(typeof(BfresFormatException).IsAssignableFrom(typeof(BfresUnsupportedException)));
    }

    private static ResFile Build(BfresPlatform platform, uint raw)
    {
        var file = new ResFile
        {
            Name = "synthetic",
            Platform = platform,
            Version = new BfresVersion(platform, raw),
            DataAlignment = 0x2000,
            Alignment = 0x0C,
            TargetAddressSize = 0x40,
        };
        file.ExternalFiles.Add("alpha.bin", new ExternalFile { Data = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray() });
        file.ExternalFiles.Add("beta.bin", new ExternalFile { Data = new byte[] { 9, 8, 7, 6, 5 } });
        return file;
    }
}
