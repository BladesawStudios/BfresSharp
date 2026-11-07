using BfresSharp.Container;
using BfresSharp.Model;
using BfresSharp.Switch.Models;
using BfresSharp.Switch.Relocation;

namespace BfresSharp.Switch.IO;

/// <summary>Where each region of a written file starts: the name pool end, the buffer data, the memory pool and the embedded files.</summary>
public readonly record struct SectionBounds(long StringEnd, long BufferBase, long PoolAt, long ExternalAt, long ExternalEnd);

/// <summary>Groups the recorded pointers into the relocation sections of a written Switch file.</summary>
/// <remarks>
/// Each pointer goes in the section its target lies in: the main data, the index data, the vertex data, the memory
/// pool or the embedded files. A file without buffers keeps empty sections in their place.
/// </remarks>
public static class SwitchSectionBuilder
{
    public const int MemoryPoolSize = 288;
    private const int SectionCount = 5;

    public static List<RelocationSection> Build(SwitchWriter writer, ResFile file, SwitchBufferLayout layout, SectionBounds bounds)
    {
        var groups = new List<long>[SectionCount].Select(_ => new List<long>()).ToArray();
        foreach (long slot in writer.Pointers.Order())
        {
            long target = (long)BitConverter.ToUInt64(writer.Out.Written[(int)slot..((int)slot + 8)]);
            groups[SectionOf(target, layout, bounds)].Add(slot);
        }

        uint stringEnd = (uint)bounds.StringEnd;
        var sections = new List<RelocationSection> { new(0, stringEnd, [.. RelocationEncoder.Encode(groups[0])]) };
        bool hasExternal = bounds.ExternalAt != 0;
        uint externalLength = hasExternal ? (uint)(bounds.ExternalEnd - bounds.ExternalAt) : 0;
        if (layout.IsEmpty)
        {
            int empty = (file.Platform == BfresPlatform.Switch2 ? 5 : 4) - (hasExternal ? 1 : 0);
            for (int unused = 0; unused < empty; unused++) sections.Add(new RelocationSection(stringEnd, 0, []));
            if (hasExternal) sections.Add(new RelocationSection((uint)bounds.ExternalAt, externalLength, [.. RelocationEncoder.Encode(groups[4])]));
            return sections;
        }

        if (layout.HasClusters)
        {
            // Cluster data sits in the section where Switch 2 files otherwise keep nothing; index and vertex sections stay empty.
            sections.Add(new RelocationSection(stringEnd, 0, []));
            sections.Add(new RelocationSection(stringEnd, 0, []));
            var inBuffer = groups[1].Concat(groups[2]).Order().ToList();
            sections.Add(new RelocationSection((uint)bounds.BufferBase, (uint)layout.TotalSize, [.. RelocationEncoder.Encode(inBuffer)]));
            sections.Add(new RelocationSection((uint)bounds.PoolAt, MemoryPoolSize, [.. RelocationEncoder.Encode(groups[3])]));
            sections.Add(new RelocationSection(hasExternal ? (uint)bounds.ExternalAt : (uint)(bounds.PoolAt + MemoryPoolSize), externalLength, [.. RelocationEncoder.Encode(groups[4])]));
            return sections;
        }

        long indexEnd = bounds.BufferBase + layout.SplitAt;
        long poolEnd = bounds.PoolAt + MemoryPoolSize;
        sections.Add(new RelocationSection((uint)bounds.BufferBase, (uint)layout.SplitAt, [.. RelocationEncoder.Encode(groups[1])]));
        sections.Add(new RelocationSection((uint)indexEnd, (uint)(layout.TotalSize - layout.SplitAt), [.. RelocationEncoder.Encode(groups[2])]));
        // Switch 2 files keep an unused section in front of the memory pool.
        if (file.Platform == BfresPlatform.Switch2) sections.Add(new RelocationSection((uint)bounds.PoolAt, 0, []));
        sections.Add(new RelocationSection((uint)bounds.PoolAt, MemoryPoolSize, [.. RelocationEncoder.Encode(groups[3])]));
        uint externalStart = hasExternal ? (uint)bounds.ExternalAt : (uint)poolEnd;
        sections.Add(new RelocationSection(externalStart, externalLength, [.. RelocationEncoder.Encode(groups[4])]));
        return sections;
    }

    private static int SectionOf(long target, SwitchBufferLayout layout, SectionBounds bounds)
    {
        if (bounds.ExternalAt != 0 && target >= bounds.ExternalAt) return 4;
        if (layout.IsEmpty || target < bounds.BufferBase) return 0;
        if (target >= bounds.PoolAt) return 3;
        return target < bounds.BufferBase + layout.SplitAt ? 1 : 2;
    }
}
