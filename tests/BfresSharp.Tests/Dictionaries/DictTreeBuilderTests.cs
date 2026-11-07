using BfresSharp.Binary;
using BfresSharp.Container;
using BfresSharp.Dictionaries;
using BfresSharp.Switch.IO;
using BfresSharp.Tests.Corpus;

namespace BfresSharp.Tests.Dictionaries;

public sealed class DictTreeBuilderTests
{
    [Fact]
    public void SwitchTreeOfOneKeyPointsBackAtItself()
    {
        IReadOnlyList<DictNode> nodes = new SwitchDictTreeBuilder().Build(["a"]);

        Assert.Equal(2, nodes.Count);
        Assert.Equal(uint.MaxValue, nodes[0].Reference);
        Assert.Equal((ushort)1, nodes[0].Left);
    }

    [Fact]
    public void WiiUDuplicateKeysAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new WiiUDictTreeBuilder().Build(["same", "same"]));
    }

    [CorpusFact]
    public void SwitchTreesRebuildFromTheirKeys()
    {
        int checkedCount = 0;
        var failures = new List<string>();
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || !header.Platform.UsesSwitchLayout()) continue;
                var reader = new SwitchReader(new ByteView(data, ByteOrder.Little), SwitchProfile.For(header.Version));
                foreach (long dictAt in RootDictionaries(reader))
                {
                    IReadOnlyList<DictNode> nodes = reader.Dictionary(dictAt);
                    if (nodes.Skip(1).Any(n => n.Hash.HasValue)) continue;
                    checkedCount++;
                    IReadOnlyList<DictNode> rebuilt = new SwitchDictTreeBuilder().Build(nodes.Skip(1).Select(n => n.Key!).ToList());
                    if (!rebuilt.Select(n => (n.Reference, n.Left, n.Right)).SequenceEqual(nodes.Select(n => (n.Reference, n.Left, n.Right))) && failures.Count < 8)
                        failures.Add($"{name} dict@{dictAt:X} ({nodes.Count - 1} keys)");
                }
            }
        }

        Assert.True(checkedCount > 0);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    // The model, animation and external file dictionaries named by the file header.
    private static IEnumerable<long> RootDictionaries(SwitchReader reader)
    {
        int shift = reader.Profile.Modern ? 0x20 : 0;
        int[] fields = [0x30, 0x40 + shift, 0x50 + shift, 0x60 + shift, 0x70 + shift, 0x80 + shift, 0xA0 + shift];
        foreach (int field in fields)
        {
            long at = reader.Pointer(field);
            if (at != 0) yield return at;
        }
    }
}
