using System.Collections;
using BfresSharp.Dictionaries;

namespace BfresSharp.Model;

/// <summary>An ordered collection of named values, the way a BFRES file keys its sections.</summary>
/// <remarks>
/// Keeps insertion order, which is the order the values are stored in the file, and looks values up by name or
/// position. Names are unique. A name the game stores as a hash instead of text keeps that hash beside the entry,
/// so it writes back as the hash it was read as; <see cref="HashOf"/> is null for ordinary names.
/// </remarks>
public sealed class ResDict<T> : IReadOnlyList<KeyValuePair<string, T>>
{
    private const int IndexThreshold = 8;
    private readonly List<string> _keys;
    private readonly List<T> _values;
    private List<ulong?>? _hashes;
    private Dictionary<string, int>? _index;

    public ResDict(int capacity = 0)
    {
        _keys = new List<string>(capacity);
        _values = new List<T>(capacity);
    }

    public int Count => _keys.Count;

    public IReadOnlyList<string> Keys => _keys;

    public IReadOnlyList<T> Values => _values;

    public KeyValuePair<string, T> this[int index] => new(_keys[index], _values[index]);

    public T this[string key]
    {
        get => _values[IndexOf(key) is var at and >= 0 ? at : throw new KeyNotFoundException($"No entry named \"{key}\"")];
        set
        {
            if (IndexOf(key) is var at and >= 0) _values[at] = value;
            else Add(key, value);
        }
    }

    public bool HasHashedKeys => _hashes is not null && _hashes.Any(hash => hash.HasValue);

    // The search tree as the file had it. A tree over hashes cannot be rebuilt without the names, so it is kept to write back.
    public IReadOnlyList<DictNode>? Layout { get; set; }

    // Whether the kept tree still describes these entries: the same hashes in the same order.
    public bool LayoutMatches => Layout is { } nodes && nodes.Count == Count + 1
        && Enumerable.Range(0, Count).All(index => nodes[index + 1].Hash == HashOf(index));

    public void Add(string key, T value, ulong? hash = null)
    {
        if (IndexOf(key) >= 0) throw new ArgumentException($"Key \"{key}\" already exists.", nameof(key));
        _index?.Add(key, _keys.Count);
        _keys.Add(key);
        _values.Add(value);
        if (hash.HasValue) (_hashes ??= [.. new ulong?[_keys.Count - 1]]).Add(hash);
        else _hashes?.Add(null);
        if (_index is null && _keys.Count > IndexThreshold) BuildIndex();
    }

    public bool ContainsKey(string key) => IndexOf(key) >= 0;

    // Short dictionaries are searched in order, which beats hashing; longer ones get an index once they outgrow that.
    public int IndexOf(string key)
    {
        if (_index is not null) return _index.TryGetValue(key, out int at) ? at : -1;
        for (int position = 0; position < _keys.Count; position++)
        {
            if (string.Equals(_keys[position], key, StringComparison.Ordinal)) return position;
        }

        return -1;
    }

    public bool TryGetValue(string key, out T value)
    {
        if (IndexOf(key) is var at and >= 0)
        {
            value = _values[at];
            return true;
        }

        value = default!;
        return false;
    }

    public ulong? HashOf(int index) => _hashes?[index];

    public void Rename(string key, string newKey)
    {
        int at = IndexOf(key);
        if (at < 0) throw new KeyNotFoundException($"No entry named \"{key}\"");
        if (key != newKey && IndexOf(newKey) >= 0) throw new ArgumentException($"Key \"{newKey}\" already exists.", nameof(newKey));
        _index?.Remove(key);
        if (_index is not null) _index[newKey] = at;
        _keys[at] = newKey;
        if (_hashes is not null) _hashes[at] = null;
    }

    public bool Remove(string key)
    {
        int at = IndexOf(key);
        if (at < 0) return false;
        RemoveAt(at);
        return true;
    }

    public void RemoveAt(int index)
    {
        _index?.Remove(_keys[index]);
        _keys.RemoveAt(index);
        _values.RemoveAt(index);
        _hashes?.RemoveAt(index);
        if (_index is null) return;
        for (int at = index; at < _keys.Count; at++) _index[_keys[at]] = at;
    }

    public void Clear()
    {
        _keys.Clear();
        _values.Clear();
        _hashes = null;
        _index = null;
    }

    private void BuildIndex()
    {
        _index = new Dictionary<string, int>(_keys.Count * 2, StringComparer.Ordinal);
        for (int position = 0; position < _keys.Count; position++) _index[_keys[position]] = position;
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        for (int at = 0; at < _keys.Count; at++) yield return new KeyValuePair<string, T>(_keys[at], _values[at]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
