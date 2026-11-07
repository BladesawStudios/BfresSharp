using System.Text;
using BfresSharp.Binary;
using BfresSharp.Dictionaries;
using BfresSharp.Errors;
using BfresSharp.Model;

namespace BfresSharp.Switch.IO;

/// <summary>Reads Switch structures: 64-bit file pointers, length-prefixed names and name dictionaries.</summary>
/// <remarks>
/// A pointer is an absolute file offset, zero meaning none. A name pointer targets a 16-bit length followed by the
/// zero-terminated characters. Every method takes absolute offsets, so a reader holds no position and can be shared.
/// </remarks>
public sealed class SwitchReader(ByteView view, SwitchProfile profile)
{
    // The magic, the linked block and the count that precede the first name of the pool.
    private const int PoolHeaderSize = 0x14;

    public ByteView View { get; } = view.WithOrder(ByteOrder.Little);

    public SwitchProfile Profile { get; } = profile;

    // Where the buffer data section starts; mesh and vertex buffer offsets are relative to it.
    public long BufferBase { get; set; }

    public long Pointer(long at)
    {
        ulong value = View.U64(at);
        if (value > (ulong)View.Length) throw new BfresFormatException($"Pointer 0x{value:X} is outside the {View.Length}-byte file", at);
        return (long)value;
    }

    // A pointer field that may hold a hash instead of an offset, as the game's models do for names it looks up later.
    public (long Target, ulong? Hash) PointerOrHash(long at)
    {
        ulong value = View.U64(at);
        return value != 0 && value > (ulong)View.Length ? (0, value) : ((long)value, null);
    }

    // A name field that holds the name's hash instead of a pointer when the file leaves the name out.
    public (string Name, ulong Hash) NameOrHash(long at)
    {
        (long target, ulong? hash) = PointerOrHash(at);
        if (hash is { } value) return ($"0x{value:X16}", value);
        return (target == 0 ? "" : StringAtTarget(target), 0);
    }

    public string? String(long pointerAt)
    {
        long target = Pointer(pointerAt);
        return target == 0 ? null : StringAtTarget(target);
    }

    public string StringAtTarget(long target)
    {
        int length = View.U16(target);
        ReadOnlySpan<byte> bytes = View.Slice(target + 2, length).Span;
        return Encoding.UTF8.GetString(bytes);
    }

    // Reads the names from the first stored name to the end of the pool block in the order stored, leaving out the empty name.
    public List<string> StringPool(long firstAt, long blockSize)
    {
        var names = new List<string>();
        long at = firstAt;
        long end = firstAt - PoolHeaderSize + blockSize;
        while (at + 2 <= end)
        {
            string text = StringAtTarget(at);
            if (text.Length > 0) names.Add(text);
            at = (at + 2 + Encoding.UTF8.GetByteCount(text) + 1 + 1) & ~1L;
        }

        return names;
    }

    public IReadOnlyList<DictNode> Dictionary(long dictAt)
    {
        int count = View.I32(dictAt + 4);
        if (count < 0 || count > ushort.MaxValue) throw new BfresFormatException($"Dictionary count {count} is out of range", dictAt);
        var nodes = new List<DictNode>(count + 1);
        for (int index = 0; index <= count; index++)
        {
            long at = dictAt + 8 + index * 16L;
            (long target, ulong? hash) = PointerOrHash(at + 8);
            string? key = target == 0 ? null : StringAtTarget(target);
            nodes.Add(new DictNode(View.U32(at), View.U16(at + 4), View.U16(at + 6), key, hash));
        }

        return nodes;
    }

    public ResDict<T> ReadDict<T>(long dictPointerAt, long arrayPointerAt, int elementSize, Func<long, T> readElement)
    {
        long dictAt = Pointer(dictPointerAt);
        if (dictAt == 0) return new ResDict<T>();
        long arrayAt = Pointer(arrayPointerAt);
        IReadOnlyList<DictNode> nodes = Dictionary(dictAt);
        var result = new ResDict<T>(nodes.Count - 1);
        for (int index = 1; index < nodes.Count; index++)
            result.Add(KeyOf(nodes[index]), readElement(arrayAt + (index - 1L) * elementSize), nodes[index].Hash);
        if (result.HasHashedKeys) result.Layout = nodes;
        return result;
    }

    // The text of a key, or the hash in hexadecimal for a key the file stores as a hash.
    public static string KeyOf(DictNode node) => node.Key ?? (node.Hash is { } hash ? $"0x{hash:X16}" : "");

    public T[] ReadArray<T>(long arrayAt, int count, int elementSize, Func<long, T> readElement)
    {
        var result = new T[count];
        for (int index = 0; index < count; index++) result[index] = readElement(arrayAt + (long)index * elementSize);
        return result;
    }
}
