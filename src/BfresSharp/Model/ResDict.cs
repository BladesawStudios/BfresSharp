using System.Collections;

namespace BfresSharp.Model;

/// <summary>An ordered collection of named values, the way a BFRES file keys its sections.</summary>
/// <remarks>
/// Keeps insertion order, which is the order the values are stored in the file, and looks values up by name or
/// position. Names are unique. A name the game stores as a hash instead of text keeps that hash beside the entry,
/// so it writes back as the hash it was read as; <see cref="HashOf"/> is null for ordinary names.
/// </remarks>
public sealed class ResDict<T> : IReadOnlyList<KeyValuePair<string, T>>
{
    private readonly List<string> _keys = [];
    private readonly List<T> _values = [];
    private readonly List<ulong?> _hashes = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);

    public int Count => _keys.Count;

    public IReadOnlyList<string> Keys => _keys;

    public IReadOnlyList<T> Values => _values;

    public KeyValuePair<string, T> this[int index] => new(_keys[index], _values[index]);

    public T this[string key]
    {
        get => _values[IndexOf(key) is var at and >= 0 ? at : throw new KeyNotFoundException($"No entry named \"{key}\"")];
        set
        {
            if (_index.TryGetValue(key, out int at)) _values[at] = value;
            else Add(key, value);
        }
    }

    public bool HasHashedKeys => _hashes.Any(hash => hash.HasValue);

    public void Add(string key, T value, ulong? hash = null)
    {
        if (!_index.TryAdd(key, _keys.Count)) throw new ArgumentException($"Key \"{key}\" already exists.", nameof(key));
        _keys.Add(key);
        _values.Add(value);
        _hashes.Add(hash);
    }

    public bool ContainsKey(string key) => _index.ContainsKey(key);

    public int IndexOf(string key) => _index.TryGetValue(key, out int at) ? at : -1;

    public bool TryGetValue(string key, out T value)
    {
        if (_index.TryGetValue(key, out int at))
        {
            value = _values[at];
            return true;
        }

        value = default!;
        return false;
    }

    public ulong? HashOf(int index) => _hashes[index];

    public void Rename(string key, string newKey)
    {
        int at = IndexOf(key);
        if (at < 0) throw new KeyNotFoundException($"No entry named \"{key}\"");
        if (key != newKey && _index.ContainsKey(newKey)) throw new ArgumentException($"Key \"{newKey}\" already exists.", nameof(newKey));
        _index.Remove(key);
        _index[newKey] = at;
        _keys[at] = newKey;
        _hashes[at] = null;
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
        _index.Remove(_keys[index]);
        _keys.RemoveAt(index);
        _values.RemoveAt(index);
        _hashes.RemoveAt(index);
        for (int at = index; at < _keys.Count; at++) _index[_keys[at]] = at;
    }

    public void Clear()
    {
        _keys.Clear();
        _values.Clear();
        _hashes.Clear();
        _index.Clear();
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        for (int at = 0; at < _keys.Count; at++) yield return new KeyValuePair<string, T>(_keys[at], _values[at]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
