namespace BfresSharp.Model.Animation;

/// <summary>How one texture slot of a material is animated: the curve or constant that selects the texture.</summary>
public sealed class PatternAnimInfo
{
    public string Name { get; set; } = "";

    // The hash the file names this entry by when it leaves the name out; zero for a named entry.
    public ulong NameHash { get; set; }

    public short CurveIndex { get; set; } = -1;

    public ushort BeginConstant { get; set; } = ushort.MaxValue;

    public sbyte SubBindIndex { get; set; } = -1;
}
