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
    private static readonly Comparison<string> Order = Compare;
    private readonly Dictionary<string, Entry> _names = new(StringComparer.Ordinal);

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
        string[] texts = [.. _names.Keys];
        Array.Sort(texts, Order);
        foreach (string text in texts)
        {
            Entry entry = _names[text];
            int length = entry.Encoding.GetByteCount(text);
            o.U32((uint)length);
            foreach (int field in entry.Fields) o.SetU32(field, (uint)(o.Length - field));
            entry.Encoding.GetBytes(text, o.Claim(length));
            o.Zeros(entry.Encoding.GetByteCount("\0")).Align(4);
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
