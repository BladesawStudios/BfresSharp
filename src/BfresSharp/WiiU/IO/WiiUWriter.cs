using System.Text;
using BfresSharp.Binary;
using BfresSharp.Model;

namespace BfresSharp.WiiU.IO;

/// <summary>Writes Wii U files: a record is registered where it is referenced and placed where the layout wants it.</summary>
/// <remarks>
/// Referencing a record, list or dictionary writes a placeholder offset and registers the record with the code that writes
/// it; nothing is written yet. <see cref="Place"/> writes a registered record at the current position, aligned to four
/// bytes, which lets a codec put each record's children where Nintendo's tools put them. <see cref="Finish"/> fills in
/// the offsets, then writes the name pool and last the data blocks. Offsets are relative to their own field.
/// </remarks>
public sealed class WiiUWriter(uint version, int alignment)
{
    private readonly Dictionary<object, Entry> _entries = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, List<Entry>> _groups = new(ReferenceEqualityComparer.Instance);
    private readonly WiiUNamePool _names = new();
    private readonly WiiUDataBlocks _blocks = new();

    public ByteBuilder Out { get; } = new(ByteOrder.Big, 1 << 16);

    public uint Version { get; } = version;

    // The alignment the file states for its data blocks.
    public int Alignment { get; } = alignment;

    // The index the record being written was registered with: its position in the list or dictionary that holds it.
    public int Index { get; private set; }

    // The position of the string pool in the finished file and its length, for the header to point at.
    public (int Start, int Size) Pool { get; private set; }

    public bool AtLeast(uint raw) => Version >= raw;

    // Registers a record and writes the offset field that points at it; null writes no offset.
    public void Item<T>(T? data, int index, Action<T, int> write)
        where T : class
    {
        if (data is null)
        {
            Out.U32(0);
            return;
        }

        Point(Register(data, index, () => write(data, Index)));
    }

    // Registers the records of a list, which are placed one after the other; the offset is that of the first.
    public void List<T>(IReadOnlyList<T> list, Action<T, int> write, bool pointWhenEmpty = false)
        where T : class
    {
        Group(list, pointWhenEmpty, (element, index) => Register(element, index, () => write(element, Index)));
    }

    // Registers an array that is pointed at even when it is empty, as the files do for skin indices and the like. Returns the key to place it with.
    public object Array(Action write)
    {
        var key = new object();
        Point(Register(key, -1, write));
        return key;
    }

    // Registers a dictionary and its values; the values are placed by PlaceValues, the dictionary itself by Place.
    public void Dict<T>(ResDict<T> dict, Action<T, int> write)
        where T : class
    {
        if (dict.Count == 0)
        {
            Out.U32(0);
            return;
        }

        if (!_groups.ContainsKey(dict))
        {
            for (int index = 0; index < dict.Count; index++)
            {
                T value = dict.Values[index];
                Register(value, index, () => write(value, Index));
            }

            _groups[dict] = [new Entry(-1, () => WriteDict(dict, value => Point(_entries[value!])))];
        }

        Point(_groups[dict][0]);
    }

    // Registers a dictionary whose values are names.
    public void NameDict(ResDict<string> dict)
    {
        if (dict.Count == 0)
        {
            Out.U32(0);
            return;
        }

        if (!_groups.ContainsKey(dict)) _groups[dict] = [new Entry(-1, () => WriteDict(dict, text => Name(text)))];
        Point(_groups[dict][0]);
    }

    // Registers the records of a list that have no identity of their own (small value records).
    public void Records<T>(IReadOnlyList<T> list, Action<T, int> write, bool pointWhenEmpty = false)
    {
        Group(list, pointWhenEmpty, (element, index) => new Entry(index, () => write(element, Index)));
    }

    // Registers arbitrary data (an array of numbers, say); null writes no offset.
    public void Custom(object? data, Action write)
    {
        if (data is null)
        {
            Out.U32(0);
            return;
        }

        Point(Register(data, -1, write));
    }

    public void Name(string? text, Encoding? encoding = null)
    {
        if (text is not null) _names.Add(text, encoding ?? Encoding.UTF8, Out.Length);
        Out.U32(0);
    }

    // Writes the offset to data that goes behind the name pool; the rank orders the blocks (index, vertex, texture, file).
    public void Block(byte[]? bytes, Layout layout)
    {
        if (bytes is not null) _blocks.Add(bytes.Length == 0 ? new object() : bytes, bytes, layout, Out.Length);
        Out.U32(0);
    }

    // Writes each registered record, list or dictionary now, unless it is already written; null is skipped.
    public void Place(params object?[] keys)
    {
        foreach (object? key in keys)
        {
            if (key is null) continue;
            if (_groups.TryGetValue(key, out List<Entry>? group)) group.ForEach(Write);
            else if (_entries.TryGetValue(key, out Entry? entry)) Write(entry);
        }
    }

    // Writes the values of a registered dictionary in order.
    public void PlaceValues<T>(ResDict<T> dict)
        where T : class
    {
        foreach (T value in dict.Values) Place(value);
    }

    // Writes a registered dictionary and then its values.
    public void PlaceAll<T>(ResDict<T> dict)
        where T : class
    {
        Place(dict);
        PlaceValues(dict);
    }

    public void Finish()
    {
        foreach (Entry entry in _entries.Values.Concat(_groups.Values.SelectMany(group => group)).Distinct())
        {
            if (entry.Target < 0 && entry.Fields.Count > 0) throw new InvalidOperationException("A record was referenced but never placed");
        }

        Pool = _names.Write(Out);
        _blocks.Write(Out);
    }

    // Writes an offset field to the entry: the distance when it is placed already, otherwise a placeholder filled in at its placement.
    private void Point(Entry entry)
    {
        if (entry.Target >= 0) Out.U32((uint)(entry.Target - Out.Length));
        else
        {
            entry.Fields.Add(Out.Length);
            Out.U32(0);
        }
    }

    private void Group<T>(IReadOnlyList<T> list, bool pointWhenEmpty, Func<T, int, Entry> make)
    {
        if (list.Count == 0 && !pointWhenEmpty)
        {
            Out.U32(0);
            return;
        }

        if (!_groups.TryGetValue(list, out List<Entry>? group))
        {
            group = [];
            for (int index = 0; index < list.Count; index++) group.Add(make(list[index], index));
            if (list.Count == 0) group.Add(new Entry(0, () => { }));
            _groups[list] = group;
        }

        Point(group[0]);
    }

    private Entry Register(object data, int index, Action write)
    {
        if (_entries.TryGetValue(data, out Entry? known)) return known;
        var entry = new Entry(index, write);
        _entries[data] = entry;
        return entry;
    }

    private void Write(Entry entry)
    {
        if (entry.Target >= 0) return;
        Out.Align(4);
        entry.Target = Out.Length;
        foreach (int field in entry.Fields) Out.SetU32(field, (uint)(entry.Target - field));
        Index = entry.Index;
        entry.Write();
    }

    private void WriteDict<T>(ResDict<T> dict, Action<T> value)
    {
        var nodes = WiiUDictCodec.Nodes(dict.Keys);
        Out.U32((uint)(8 + nodes.Count * 16)).U32((uint)dict.Count);
        for (int index = 0; index < nodes.Count; index++)
        {
            Out.U32(nodes[index].Reference).U16(nodes[index].Left).U16(nodes[index].Right);
            Name(index == 0 ? null : dict.Keys[index - 1]);
            if (index == 0) Out.U32(0);
            else value(dict.Values[index - 1]);
        }
    }

    private sealed class Entry(int index, Action write)
    {
        public List<int> Fields { get; } = [];

        public int Target { get; set; } = -1;

        public int Index => index;

        public Action Write => write;
    }
}
