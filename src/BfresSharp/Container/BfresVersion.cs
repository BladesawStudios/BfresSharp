namespace BfresSharp.Container;

/// <summary>The format version stored in a BFRES header, split into its parts for the platform it came from.</summary>
/// <remarks>
/// Wii U packs four bytes, written Major.Minor.Micro.Revision (such as 3.5.0.3). Switch packs a 16-bit major
/// and two bytes, written Major.Minor.Micro (such as 10.0.0), and has no revision. <see cref="Raw"/> is the
/// word exactly as stored, so a version always writes back unchanged.
/// </remarks>
public readonly record struct BfresVersion(BfresPlatform Platform, uint Raw) : IComparable<BfresVersion>
{
    public int Major => (int)(Platform == BfresPlatform.Switch ? Raw >> 16 : Raw >> 24);

    public int Minor => (int)(Platform == BfresPlatform.Switch ? (Raw >> 8) & 0xFF : (Raw >> 16) & 0xFF);

    public int Micro => (int)(Platform == BfresPlatform.Switch ? Raw & 0xFF : (Raw >> 8) & 0xFF);

    public int Revision => Platform == BfresPlatform.Switch ? 0 : (int)(Raw & 0xFF);

    public int CompareTo(BfresVersion other) => Raw.CompareTo(other.Raw);

    public override string ToString()
    {
        return Platform == BfresPlatform.Switch ? $"{Major}.{Minor}.{Micro}" : $"{Major}.{Minor}.{Micro}.{Revision}";
    }
}
