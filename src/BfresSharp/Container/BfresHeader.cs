using BfresSharp.Binary;

namespace BfresSharp.Container;

/// <summary>What the first bytes of a BFRES file say about the file as a whole.</summary>
/// <remarks>
/// Holds only what every reader needs before it can choose a layout: the platform, the byte order the rest of
/// the file is in, the version, the size the file claims and the alignment of its data blocks. Reading one
/// validates the magic and byte order mark, so a header in hand means the file really is a BFRES.
/// </remarks>
public sealed record BfresHeader(
    BfresPlatform Platform,
    ByteOrder ByteOrder,
    BfresVersion Version,
    uint FileSize,
    uint DataAlignment);
