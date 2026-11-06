namespace BfresSharp.Switch.Relocation;

/// <summary>A span of a Switch file whose pointers are listed together in the relocation table.</summary>
/// <remarks>
/// A file has five sections: everything from the start through the string pool, the index buffers, the vertex
/// buffers, the memory pool and the embedded files. A section nothing lives in still has an entry, positioned
/// where the previous one ended and with a size of zero.
/// </remarks>
public sealed record RelocationSection(uint Position, uint Size, IReadOnlyList<RelocationEntry> Entries)
{
    public IEnumerable<long> Pointers() => Entries.SelectMany(entry => entry.Pointers());
}
