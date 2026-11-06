using BfresSharp.Binary;
using BfresSharp.Container;
using BfresSharp.Switch.Relocation;
using BfresSharp.Tests.Corpus;

namespace BfresSharp.Tests.Switch;

public sealed class RelocationTableTests
{
    [Fact]
    public void EntryExpandsToPointersWithPadding()
    {
        var entry = new RelocationEntry(Position: 0x100, StructCount: 2, OffsetCount: 2, PaddingCount: 1);

        Assert.Equal([0x100L, 0x108, 0x118, 0x120], entry.Pointers());
    }

    [Fact]
    public void WritesWhatItReads()
    {
        var table = new RelocationTable([
            new RelocationSection(0, 0x300, [new RelocationEntry(0x20, 1, 3, 0), new RelocationEntry(0x80, 4, 2, 2)]),
            new RelocationSection(0x300, 0, []),
        ]);
        byte[] bytes = table.ToBytes(0);

        RelocationTable read = RelocationTable.Read(new ByteView(bytes, ByteOrder.Little), 0);

        Assert.Equal(table.Sections.Count, read.Sections.Count);
        Assert.Equal(table.Sections[0].Entries, read.Sections[0].Entries);
        Assert.Equal(bytes, read.ToBytes(0));
    }

    [CorpusFact]
    public void CorpusEntriesAreRebuiltFromTheirPointers()
    {
        int sections = 0;
        var failures = new List<string>();
        foreach (string folder in Corpus.Corpus.Folders)
        {
            foreach ((string name, byte[] data) in Corpus.Corpus.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || !header.Platform.UsesSwitchLayout()) continue;
                var view = new ByteView(data, ByteOrder.Little);
                RelocationTable table = RelocationTable.Read(view, view.U32(0x18));
                foreach (RelocationSection section in table.Sections)
                {
                    sections++;
                    List<RelocationEntry> rebuilt = RelocationEncoder.Encode(section.Pointers().ToList());
                    if (!rebuilt.SequenceEqual(section.Entries) && failures.Count < 12)
                        failures.Add($"{name}: {Describe(section.Entries, rebuilt)}");
                }
            }
        }

        Assert.True(sections > 0);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static string Describe(IReadOnlyList<RelocationEntry> expected, IReadOnlyList<RelocationEntry> actual)
    {
        int at = 0;
        while (at < expected.Count && at < actual.Count && expected[at] == actual[at]) at++;
        string e = at < expected.Count ? expected[at].ToString() : "end";
        string a = at < actual.Count ? actual[at].ToString() : "end";
        return $"entry {at} of {expected.Count}: expected {e}, rebuilt {a}";
    }

    [CorpusFact]
    public void CorpusTablesWriteBackIdentically()
    {
        var failures = new List<string>();
        foreach (string folder in Corpus.Corpus.Folders)
        {
            foreach ((string name, byte[] data) in Corpus.Corpus.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || !header.Platform.UsesSwitchLayout()) continue;
                var view = new ByteView(data, ByteOrder.Little);
                uint offset = view.U32(0x18);
                RelocationTable table = RelocationTable.Read(view, offset);
                if (!data.AsSpan((int)offset, table.ByteLength).SequenceEqual(table.ToBytes(offset)))
                    failures.Add($"{folder}/{name}");
            }
        }

        Assert.Empty(failures);
    }
}
