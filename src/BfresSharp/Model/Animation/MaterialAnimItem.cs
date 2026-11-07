namespace BfresSharp.Model.Animation;

/// <summary>The animation of one material: its parameter and texture pattern entries with the curves and constants they use.</summary>
/// <remarks>
/// The curve and constant indices record where this material's shader parameter, texture pattern and visibility
/// entries start in <see cref="Curves"/> and <see cref="Constants"/>; they are kept as read.
/// </remarks>
public sealed class MaterialAnimItem
{
    public string Name { get; set; } = "";

    // The hash the file names this entry by when it leaves the name out; zero for a named entry.
    public ulong NameHash { get; set; }

    public ushort ShaderParamCurveIndex { get; set; }

    public ushort TexturePatternCurveIndex { get; set; }

    public ushort VisualConstantIndex { get; set; }

    public ushort VisualCurveIndex { get; set; }

    public ushort BeginVisualConstantIndex { get; set; }

    // Wii U files also state the index of the first parameter entry and, for patterns, the base value of each entry.
    public int InfoIndex { get; set; }

    public ushort[] BaseDataList { get; set; } = [];

    public List<ParamAnimInfo> ParamAnimInfos { get; set; } = [];

    public List<PatternAnimInfo> PatternAnimInfos { get; set; } = [];

    public List<AnimCurve> Curves { get; set; } = [];

    public List<AnimConstant> Constants { get; set; } = [];

    // The header counts a parameter for each constant and curve of the parameter entries, and each pattern entry once.
    public int ParamCount => ParamAnimInfos.Sum(info => info.ConstantCount + info.FloatCurveCount + info.IntCurveCount);

    public int PatternCount => PatternAnimInfos.Count;

    // The curves and constants no parameter or pattern entry claims animate visibility; the header counts them.
    public int VisualCount
    {
        get
        {
            int curves = Curves.Count - ParamAnimInfos.Sum(info => info.FloatCurveCount + info.IntCurveCount) - PatternAnimInfos.Count(info => info.CurveIndex >= 0);
            int constants = Constants.Count - ParamAnimInfos.Sum(info => (int)info.ConstantCount) - PatternAnimInfos.Count(info => info.CurveIndex < 0);
            return Math.Max(0, curves) + Math.Max(0, constants);
        }
    }
}
