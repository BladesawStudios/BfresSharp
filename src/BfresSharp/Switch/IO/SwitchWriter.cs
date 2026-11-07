using System.Text;
using BfresSharp.Binary;
using BfresSharp.Switch.Relocation;

namespace BfresSharp.Switch.IO;

/// <summary>Builds a Switch file: appends structures, fills in pointers once their targets are placed, and keeps the pool of names.</summary>
/// <remarks>
/// Every pointer field is created through <see cref="Pointer()"/> or <see cref="StringPointer"/>, which record its
/// offset, so the relocation table is derived from them and never listed by hand. A null pointer is still a pointer
/// field and is listed. Names are collected and written once, in ordinal order, by <see cref="WriteStringPool"/>.
/// </remarks>
public sealed class SwitchWriter(SwitchProfile profile)
{
    private readonly List<long> _pointers = [];
    private readonly SortedDictionary<string, List<long>> _strings = new(StringComparer.Ordinal);

    public SwitchProfile Profile { get; } = profile;

    public ByteBuilder Out { get; } = new(ByteOrder.Little, 1 << 16);

    public int Position => Out.Length;

    public IReadOnlyList<long> Pointers => _pointers;

    public IReadOnlyDictionary<string, List<long>> Strings => _strings;

    // File offsets just after the magic of every section that carries a linked header block, in file order.
    public List<int> BlockPositions { get; } = [];

    // Reserves the 12-byte linked header block that follows a magic in files before version 9; the file writer fills it in.
    public void Block()
    {
        BlockPositions.Add(Position);
        Out.U32(0).U64(0);
    }

    public SwitchWriter Align(int alignment)
    {
        Out.Align(alignment);
        return this;
    }

    // A pointer field to be filled in later; it is listed in the relocation table.
    public int Pointer()
    {
        int slot = Position;
        _pointers.Add(slot);
        Out.U64(0);
        return slot;
    }

    public int Pointer(long target)
    {
        int slot = Pointer();
        Out.SetU64(slot, (ulong)target);
        return slot;
    }

    // Points a slot at the current position.
    public void Resolve(int slot) => Out.SetU64(slot, (ulong)Position);

    public void ResolveTo(int slot, long target) => Out.SetU64(slot, (ulong)target);

    // A pointer field that holds no pointer and is not listed in the relocation table.
    public int Unlisted(ulong value = 0)
    {
        int slot = Position;
        Out.U64(value);
        return slot;
    }

    public int StringPointer(string? value)
    {
        int slot = Pointer();
        if (value is not null) QueueString(slot, value);
        return slot;
    }

    public void QueueString(int slot, string value)
    {
        if (!_strings.TryGetValue(value, out List<long>? slots)) _strings[value] = slots = [];
        slots.Add(slot);
    }

    // Writes every queued name, filling in the pointers to it; returns where each name was placed.
    public Dictionary<string, int> WriteStringPool(IReadOnlyList<string> preferredOrder)
    {
        QueueSentinel();
        var placed = new Dictionary<string, int>(_strings.Count, StringComparer.Ordinal);
        foreach ((string text, List<long> slots) in InPoolOrder(preferredOrder))
        {
            Align(2);
            placed[text] = Position;
            foreach (long slot in slots) Out.SetU64((int)slot, (ulong)Position);
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            Out.U16((ushort)bytes.Length).Bytes(bytes).U8(0);
        }

        Align(2);
        return placed;
    }

    // The empty name comes first; names the file was read with keep their order; new names follow in ordinal order.
    private IEnumerable<KeyValuePair<string, List<long>>> InPoolOrder(IReadOnlyList<string> preferredOrder)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { "" };
        yield return new("", _strings[""]);
        foreach (string text in preferredOrder)
        {
            if (_strings.TryGetValue(text, out List<long>? slots) && seen.Add(text)) yield return new(text, slots);
        }

        foreach ((string text, List<long> slots) in _strings)
        {
            if (seen.Add(text)) yield return new(text, slots);
        }
    }

    // The empty name is always in the pool, whether or not a pointer uses it.
    private void QueueSentinel()
    {
        if (!_strings.ContainsKey("")) _strings[""] = [];
    }
}
