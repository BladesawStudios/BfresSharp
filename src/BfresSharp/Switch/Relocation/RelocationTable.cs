using BfresSharp.Binary;
using BfresSharp.Errors;

namespace BfresSharp.Switch.Relocation;

/// <summary>The "_RLT" block at the end of a Switch file, listing every pointer in it by section.</summary>
/// <remarks>
/// Layout: the "_RLT" magic, the table's own file offset, the section count and a padding word; then one 24-byte
/// record per section (8 bytes of padding, position, size, index of its first entry, entry count); then all entries
/// of all sections in order, each 8 bytes (position, struct count, offset count, padding count). Reading keeps
/// everything needed to write the same bytes back.
/// </remarks>
public sealed class RelocationTable
{
    private const int HeaderSize = 16;
    private const int SectionRecordSize = 24;
    private const int EntrySize = 8;

    public RelocationTable(IReadOnlyList<RelocationSection> sections)
    {
        Sections = sections;
    }

    public IReadOnlyList<RelocationSection> Sections { get; }

    public int ByteLength => HeaderSize + Sections.Count * SectionRecordSize + Sections.Sum(s => s.Entries.Count) * EntrySize;

    public static RelocationTable Read(ByteView view, long offset)
    {
        if (!view.StartsWith(offset, "_RLT"u8))
            throw new BfresFormatException("Relocation table does not start with _RLT", offset);

        view = view.WithOrder(ByteOrder.Little);
        uint sectionCount = view.U32(offset + 8);
        long entriesAt = offset + HeaderSize + sectionCount * SectionRecordSize;
        var sections = new List<RelocationSection>((int)sectionCount);
        for (int index = 0; index < sectionCount; index++)
            sections.Add(ReadSection(view, offset + HeaderSize + index * SectionRecordSize, entriesAt));

        return new RelocationTable(sections);
    }

    public byte[] ToBytes(uint tableOffset)
    {
        var writer = new ByteBuilder(ByteOrder.Little, ByteLength);
        writer.Bytes("_RLT"u8);
        writer.U32(tableOffset);
        writer.U32((uint)Sections.Count);
        writer.U32(0);

        int entryIndex = 0;
        foreach (RelocationSection section in Sections)
        {
            writer.U64(0);
            writer.U32(section.Position);
            writer.U32(section.Size);
            writer.U32((uint)entryIndex);
            writer.U32((uint)section.Entries.Count);
            entryIndex += section.Entries.Count;
        }

        foreach (RelocationEntry entry in Sections.SelectMany(s => s.Entries))
        {
            writer.U32(entry.Position);
            writer.U16((ushort)entry.StructCount);
            writer.U8((byte)entry.OffsetCount);
            writer.U8((byte)entry.PaddingCount);
        }

        return writer.ToArray();
    }

    private static RelocationSection ReadSection(ByteView view, long recordAt, long entriesAt)
    {
        uint position = view.U32(recordAt + 8);
        uint size = view.U32(recordAt + 12);
        uint first = view.U32(recordAt + 16);
        uint count = view.U32(recordAt + 20);
        var entries = new List<RelocationEntry>((int)count);
        for (uint index = 0; index < count; index++)
        {
            long at = entriesAt + (first + index) * EntrySize;
            entries.Add(new RelocationEntry(view.U32(at), view.U16(at + 4), view.U8(at + 6), view.U8(at + 7)));
        }

        return new RelocationSection(position, size, entries);
    }
}
