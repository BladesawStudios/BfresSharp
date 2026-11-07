using BfresSharp.Container;

namespace BfresSharp.Switch.IO;

/// <summary>The layout choices a Switch file's version makes, decided once from the version.</summary>
/// <remarks>
/// Version 9 and later drop the linked header blocks that follow the magic of every section, put the flags there
/// instead, and add reserved space to the file header. Readers and writers ask the profile rather than comparing
/// version numbers.
/// </remarks>
public sealed record SwitchProfile(BfresVersion Version, bool Modern)
{
    public static SwitchProfile For(BfresVersion version) => new(version, version.Major >= 9);

    public bool HasHeaderBlocks => !Modern;
}
