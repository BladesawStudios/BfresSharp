using System.Text;
using BfresSharp.Binary;

namespace BfresSharp.WiiU.IO;

/// <summary>Collects the names a Wii U file refers to and writes them as one pool in ordinal order.</summary>
/// <remarks>
/// Each name is stored once, as its byte length, the characters, a terminator and padding to four bytes. An offset to a
/// name points at the first character. The empty name sorts last, as the files Nintendo's tools write do.
/// </remarks>
internal sealed class WiiUNamePool
{
    private static readonly IComparer<string> Order = Comparer<string>.Create(Compare);
    private readonly SortedDictionary<string, Entry> _names = new(Order);

    public void Add(string text, Encoding encoding, int field)
    {
        if (!_names.TryGetValue(text, out Entry? entry)) _names[text] = entry = new Entry(encoding);
        entry.Fields.Add(field);
    }

    // Writes the pool at the current position, fills in every offset to a name and returns where the pool starts and its size.
    public (int Start, int Size) Write(ByteBuilder o)
    {
        o.Align(4);
        int start = o.Length;
        foreach ((string text, Entry entry) in _names)
        {
            byte[] bytes = entry.Encoding.GetBytes(text);
            o.U32((uint)bytes.Length);
            foreach (int field in entry.Fields) o.SetU32(field, (uint)(o.Length - field));
            o.Bytes(bytes).Zeros(entry.Encoding.GetByteCount("\0")).Align(4);
        }

        return (start, o.Length - start);
    }

    private static int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y) || (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))) return 0;
        if (string.IsNullOrEmpty(x)) return 1;
        if (string.IsNullOrEmpty(y)) return -1;
        return string.CompareOrdinal(x, y);
    }

    private sealed class Entry(Encoding encoding)
    {
        public List<int> Fields { get; } = [];

        public Encoding Encoding { get; } = encoding;
    }
}
