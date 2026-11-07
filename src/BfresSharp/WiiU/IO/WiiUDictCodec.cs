using BfresSharp.Dictionaries;

namespace BfresSharp.WiiU.IO;

/// <summary>Builds the dictionary trees of a Wii U file.</summary>
/// <remarks>
/// A dictionary is a size word, the key count and one 16-byte node per key plus the root: reference, left and right
/// indices, and offsets to the key's name and to its value. The tree is built from the keys, so a dictionary always
/// matches its entries.
/// </remarks>
public static class WiiUDictCodec
{
    private static readonly WiiUDictTreeBuilder Builder = new();

    public static IReadOnlyList<DictNode> Nodes(IReadOnlyList<string> keys) => Builder.Build(keys);
}
