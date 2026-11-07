namespace BfresSharp.Dictionaries;

/// <summary>Builds the dictionary tree of a Wii U file.</summary>
/// <remarks>
/// Numbers the bits of a key from its first character: bit 0 is the lowest bit of the first character and each
/// character holds eight bits. The reference of a new node is the highest bit at which its key and the closest
/// existing key differ. The root has reference 0xFFFFFFFF and no key.
/// </remarks>
public sealed class WiiUDictTreeBuilder : IDictTreeBuilder
{
    private const uint RootReference = uint.MaxValue;

    public IReadOnlyList<DictNode> Build(IReadOnlyList<string> keys)
    {
        int count = keys.Count + 1;
        string[] names = new string[count];
        uint[] reference = new uint[count];
        ushort[] left = new ushort[count];
        ushort[] right = new ushort[count];
        names[0] = "";
        reference[0] = RootReference;
        for (int i = 1; i < count; i++) names[i] = keys[i - 1];

        for (ushort i = 1; i < count; i++) Insert(names, reference, left, right, i);

        var built = new List<DictNode>(count);
        for (int i = 0; i < count; i++) built.Add(new DictNode(reference[i], left[i], right[i], i == 0 ? null : names[i]));
        return built;
    }

    private static void Insert(string[] names, uint[] reference, ushort[] left, ushort[] right, ushort index)
    {
        string key = names[index];
        int parent = 0;
        int child = left[0];
        while (reference[parent] > reference[child])
        {
            parent = child;
            child = Direction(key, reference[child]) == 1 ? right[child] : left[child];
        }

        uint differing = (uint)Math.Max(key.Length, names[child].Length) * 8;
        while (Direction(names[child], differing) == Direction(key, differing))
        {
            if (differing == 0) throw new ArgumentException($"Duplicate key \"{key}\" in dictionary.");
            differing--;
        }

        reference[index] = differing;
        parent = 0;
        child = left[0];
        while (reference[parent] > reference[child] && reference[child] > differing)
        {
            parent = child;
            child = Direction(key, reference[child]) == 1 ? right[child] : left[child];
        }

        if (Direction(key, differing) == 1)
        {
            left[index] = (ushort)child;
            right[index] = index;
        }
        else
        {
            left[index] = index;
            right[index] = (ushort)child;
        }

        if (Direction(key, reference[parent]) == 1) right[parent] = index;
        else left[parent] = index;
    }

    private static int Direction(string name, uint reference)
    {
        int character = (int)(reference >> 3);
        return character < name.Length ? (name[character] >> (int)(reference & 7)) & 1 : 0;
    }
}
