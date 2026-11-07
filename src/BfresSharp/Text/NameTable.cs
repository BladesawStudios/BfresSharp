using System.Globalization;

namespace BfresSharp.Text;

/// <summary>Turns the hashes a file holds in place of names into names and back, with a table of known names.</summary>
/// <remarks>
/// A name the file left out is read as 0x and sixteen hex digits. With a table (the game's ExternalBinaryString file),
/// the text shows the name instead, and a name in the table is stored as its hash again. Without one, or for a hash
/// the table lacks, the hex form is used both ways.
/// </remarks>
internal sealed class NameTable(IReadOnlyDictionary<ulong, string>? byHash)
{
    private Dictionary<string, ulong>? _byName;

    private Dictionary<string, ulong> ByName => _byName ??= (byHash ?? new Dictionary<ulong, string>())
        .GroupBy(entry => entry.Value).ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);

    public string? Show(string name, ulong hash)
        => name.Length == 0 ? null : hash != 0 && byHash is not null && byHash.TryGetValue(hash, out string? known) ? known : name;

    public string Store(string? name)
        => name is null ? "" : ByName.TryGetValue(name, out ulong hash) ? $"0x{hash:X16}" : name;

    public ulong HashOf(string? name)
    {
        if (name is null) return 0;
        if (ByName.TryGetValue(name, out ulong hash)) return hash;
        return name is { Length: 18 } && name.StartsWith("0x", StringComparison.Ordinal) && ulong.TryParse(name.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out hash) ? hash : 0;
    }
}
