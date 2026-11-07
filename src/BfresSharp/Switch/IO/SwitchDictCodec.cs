using BfresSharp.Dictionaries;
using BfresSharp.Errors;
using BfresSharp.Model;

namespace BfresSharp.Switch.IO;

/// <summary>Writes the name dictionary that goes with an array of named sections.</summary>
/// <remarks>
/// A dictionary starts with a zero word and the key count, then one 16-byte node per key plus the root: reference,
/// left and right indices, and a pointer to the key's name. The root's name is the empty string. The tree is built from
/// the keys, so a dictionary always matches its entries.
/// </remarks>
public static class SwitchDictCodec
{
    private static readonly SwitchDictTreeBuilder Builder = new();

    public static void Write<T>(SwitchWriter writer, ResDict<T> dict)
    {
        if (dict.HasHashedKeys) throw new BfresFormatException("Writing a dictionary keyed by hashes is not supported yet");
        IReadOnlyList<DictNode> nodes = Builder.Build(dict.Keys);
        writer.Out.U32(0).U32((uint)dict.Count);
        foreach (DictNode node in nodes)
        {
            writer.Out.U32(node.Reference).U16(node.Left).U16(node.Right);
            writer.StringPointer(node.Key ?? "");
        }
    }
}
