namespace BfresSharp.Dictionaries;

/// <summary>Builds the search tree of a name dictionary for the platform it is written for.</summary>
/// <remarks>
/// The tree is a Patricia trie over the bits of the keys, built by adding the keys in order, so the node of the
/// n-th key has index n. The two platforms number bits differently, so each has its own builder; building from the
/// keys a file holds gives back the nodes the file holds.
/// </remarks>
public interface IDictTreeBuilder
{
    IReadOnlyList<DictNode> Build(IReadOnlyList<string> keys);
}
