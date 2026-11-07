using System.Numerics;
using System.Text;

namespace BfresSharp.Dictionaries;

/// <summary>Builds the dictionary tree of a Switch file.</summary>
/// <remarks>
/// Treats each key as a big-endian number made of its UTF-8 bytes, so bit 0 is the lowest bit of the last byte, and
/// adds the keys one at a time to a Patricia trie. A node's reference is the number of the bit it tests, and its two
/// children are the nodes followed when that bit is clear or set. The root tests no bit and has reference
/// 0xFFFFFFFF. The nodes live in parallel arrays and the keys share one buffer, so a tree costs a handful of
/// allocations however many keys it has.
/// </remarks>
public sealed class SwitchDictTreeBuilder : IDictTreeBuilder
{
    public IReadOnlyList<DictNode> Build(IReadOnlyList<string> keys)
    {
        var trie = new Trie(keys);
        for (int key = 0; key < keys.Count; key++) trie.Insert(key + 1);

        var built = new DictNode[keys.Count + 1];
        for (int index = 0; index <= keys.Count; index++)
        {
            built[index] = new DictNode(unchecked((uint)trie.BitIndex[index]), (ushort)trie.Child[index * 2], (ushort)trie.Child[index * 2 + 1], index == 0 ? null : keys[index - 1]);
        }

        return built;
    }

    // The nodes of the trie, numbered as the keys are: node 0 is the root and node n holds the n-th key.
    private sealed class Trie
    {
        private readonly byte[] _bytes;
        private readonly int[] _start;
        private readonly int[] _length;
        private readonly int[] _parent;

        public Trie(IReadOnlyList<string> keys)
        {
            int count = keys.Count + 1;
            _start = new int[count + 1];
            for (int index = 1; index < count; index++) _start[index + 1] = _start[index] + Encoding.UTF8.GetByteCount(keys[index - 1]);
            _bytes = new byte[_start[count]];
            _length = new int[count];
            for (int index = 1; index < count; index++)
            {
                _length[index] = Encoding.UTF8.GetBytes(keys[index - 1], _bytes.AsSpan(_start[index], _start[index + 1] - _start[index]));
            }

            BitIndex = new int[count];
            BitIndex[0] = -1;
            _parent = new int[count];
            Child = new int[count * 2];
        }

        public int[] BitIndex { get; }

        // Two entries per node: the child followed when the tested bit is clear and the one followed when it is set.
        public int[] Child { get; }

        public void Insert(int node)
        {
            ReadOnlySpan<byte> data = DataOf(node);
            int current = Search(data);
            int bitIndex = Mismatch(DataOf(current), data);
            while (bitIndex < BitIndex[_parent[current]]) current = _parent[current];

            if (bitIndex < BitIndex[current])
            {
                Create(node, bitIndex, _parent[current]);
                Child[node * 2 + (Bit(data, bitIndex) ^ 1)] = current;
                Child[_parent[current] * 2 + Bit(data, BitIndex[_parent[current]])] = node;
                _parent[current] = node;
            }
            else if (bitIndex > BitIndex[current])
            {
                Create(node, bitIndex, current);
                Child[node * 2 + (Bit(data, bitIndex) ^ 1)] = Bit(DataOf(current), bitIndex) == (Bit(data, bitIndex) ^ 1) ? current : 0;
                Child[current * 2 + Bit(data, BitIndex[current])] = node;
            }
            else
            {
                int existing = Child[current * 2 + Bit(data, bitIndex)];
                int newBit = existing != 0 ? Mismatch(DataOf(existing), data) : LowestSetBit(data);
                Create(node, newBit, current);
                Child[node * 2 + (Bit(data, newBit) ^ 1)] = existing;
                Child[current * 2 + Bit(data, bitIndex)] = node;
            }
        }

        private ReadOnlySpan<byte> DataOf(int node) => _bytes.AsSpan(_start[node], _length[node]);

        // A new node starts with both children pointing back at itself.
        private void Create(int node, int bitIndex, int parent)
        {
            BitIndex[node] = bitIndex;
            _parent[node] = parent;
            Child[node * 2] = node;
            Child[node * 2 + 1] = node;
        }

        // The last node reached before the walk loops back up the tree.
        private int Search(ReadOnlySpan<byte> data)
        {
            if (Child[0] == 0) return 0;
            int node = Child[0];
            int previous;
            do
            {
                previous = node;
                node = Child[node * 2 + Bit(data, BitIndex[node])];
            }
            while (BitIndex[node] > BitIndex[previous]);
            return previous;
        }
    }

    private static int Bit(ReadOnlySpan<byte> data, int bit)
    {
        if (bit < 0) return 0;
        int fromEnd = bit >> 3;
        return fromEnd < data.Length ? (data[data.Length - 1 - fromEnd] >> (bit & 7)) & 1 : 0;
    }

    private static int Mismatch(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        int length = Math.Max(a.Length, b.Length);
        for (int fromEnd = 0; fromEnd < length; fromEnd++)
        {
            int difference = ByteFromEnd(a, fromEnd) ^ ByteFromEnd(b, fromEnd);
            if (difference != 0) return fromEnd * 8 + BitOperations.TrailingZeroCount(difference);
        }

        return -1;
    }

    private static int LowestSetBit(ReadOnlySpan<byte> data)
    {
        for (int fromEnd = 0; fromEnd < data.Length; fromEnd++)
        {
            int value = ByteFromEnd(data, fromEnd);
            if (value != 0) return fromEnd * 8 + BitOperations.TrailingZeroCount(value);
        }

        throw new InvalidOperationException("An empty key has no bit set.");
    }

    private static int ByteFromEnd(ReadOnlySpan<byte> data, int fromEnd) => fromEnd < data.Length ? data[data.Length - 1 - fromEnd] : 0;
}
