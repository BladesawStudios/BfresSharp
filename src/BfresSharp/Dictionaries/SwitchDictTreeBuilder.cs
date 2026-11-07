using System.Numerics;
using System.Text;

namespace BfresSharp.Dictionaries;

/// <summary>Builds the dictionary tree of a Switch file.</summary>
/// <remarks>
/// Treats each key as a big-endian number made of its UTF-8 bytes, so bit 0 is the lowest bit of the last byte, and
/// adds the keys one at a time to a Patricia trie. A node's reference is the number of the bit it tests, and its two
/// children are the nodes followed when that bit is clear or set. The root tests no bit and has reference
/// 0xFFFFFFFF.
/// </remarks>
public sealed class SwitchDictTreeBuilder : IDictTreeBuilder
{
    public IReadOnlyList<DictNode> Build(IReadOnlyList<string> keys)
    {
        var root = new TrieNode([], -1, null);
        var nodes = new List<TrieNode> { root };
        foreach (string key in keys)
        {
            TrieNode created = Insert(root, Encoding.UTF8.GetBytes(key));
            created.Index = nodes.Count;
            nodes.Add(created);
        }

        var built = new List<DictNode>(nodes.Count);
        for (int index = 0; index < nodes.Count; index++)
        {
            TrieNode node = nodes[index];
            built.Add(new DictNode(
                unchecked((uint)node.BitIndex),
                (ushort)node.Child[0].Index,
                (ushort)node.Child[1].Index,
                index == 0 ? null : keys[index - 1]));
        }

        return built;
    }

    private static TrieNode Insert(TrieNode root, byte[] data)
    {
        TrieNode current = Search(root, data);
        int bitIndex = Mismatch(current.Data, data);
        while (bitIndex < current.Parent.BitIndex) current = current.Parent;

        TrieNode created;
        if (bitIndex < current.BitIndex)
        {
            created = new TrieNode(data, bitIndex, current.Parent);
            created.Child[Bit(data, bitIndex) ^ 1] = current;
            current.Parent.Child[Bit(data, current.Parent.BitIndex)] = created;
            current.Parent = created;
        }
        else if (bitIndex > current.BitIndex)
        {
            created = new TrieNode(data, bitIndex, current);
            created.Child[Bit(data, bitIndex) ^ 1] = Bit(current.Data, bitIndex) == (Bit(data, bitIndex) ^ 1) ? current : root;
            current.Child[Bit(data, current.BitIndex)] = created;
        }
        else
        {
            TrieNode existing = current.Child[Bit(data, bitIndex)];
            int newBit = existing != root ? Mismatch(existing.Data, data) : LowestSetBit(data);
            created = new TrieNode(data, newBit, current);
            created.Child[Bit(data, newBit) ^ 1] = existing;
            current.Child[Bit(data, bitIndex)] = created;
        }

        return created;
    }

    // The last node reached before the walk loops back up the tree.
    private static TrieNode Search(TrieNode root, byte[] data)
    {
        if (root.Child[0] == root) return root;
        TrieNode node = root.Child[0];
        TrieNode previous;
        do
        {
            previous = node;
            node = node.Child[Bit(data, node.BitIndex)];
        }
        while (node.BitIndex > previous.BitIndex);
        return previous;
    }

    private static int Bit(byte[] data, int bit)
    {
        if (bit < 0) return 0;
        int fromEnd = bit >> 3;
        return fromEnd < data.Length ? (data[data.Length - 1 - fromEnd] >> (bit & 7)) & 1 : 0;
    }

    private static int Mismatch(byte[] a, byte[] b)
    {
        int length = Math.Max(a.Length, b.Length);
        for (int fromEnd = 0; fromEnd < length; fromEnd++)
        {
            int difference = ByteFromEnd(a, fromEnd) ^ ByteFromEnd(b, fromEnd);
            if (difference != 0) return fromEnd * 8 + BitOperations.TrailingZeroCount(difference);
        }

        return -1;
    }

    private static int LowestSetBit(byte[] data)
    {
        for (int fromEnd = 0; fromEnd < data.Length; fromEnd++)
        {
            int value = ByteFromEnd(data, fromEnd);
            if (value != 0) return fromEnd * 8 + BitOperations.TrailingZeroCount(value);
        }

        throw new InvalidOperationException("An empty key has no bit set.");
    }

    private static int ByteFromEnd(byte[] data, int fromEnd) => fromEnd < data.Length ? data[data.Length - 1 - fromEnd] : 0;

    private sealed class TrieNode
    {
        public TrieNode(byte[] data, int bitIndex, TrieNode? parent)
        {
            Data = data;
            BitIndex = bitIndex;
            Parent = parent ?? this;
            Child = [this, this];
        }

        public byte[] Data { get; }

        public int Index { get; set; }

        public int BitIndex { get; }

        public TrieNode Parent { get; set; }

        public TrieNode[] Child { get; }
    }
}
