namespace BfresSharp.Dictionaries;

/// <summary>One node of a BFRES name dictionary: a position in the search tree and the key it belongs to.</summary>
/// <remarks>
/// The first node is the root and has no key. Every other node corresponds to the entry of the same index minus
/// one. A key stored as a hash has no text and keeps the hash. <see cref="Reference"/> is the bit the node tests, and the two indices are the nodes to follow when the
/// bit is clear or set.
/// </remarks>
public readonly record struct DictNode(uint Reference, ushort Left, ushort Right, string? Key, ulong? Hash = null);
