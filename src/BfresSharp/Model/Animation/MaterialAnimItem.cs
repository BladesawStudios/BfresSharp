namespace BfresSharp.Model.Animation;

/// <summary>The animation of one material: its parameter and texture pattern entries with the curves and constants they use.</summary>
/// <remarks>
/// The curve and constant indices record where this material's shader parameter, texture pattern and visibility
/// entries start in <see cref="Curves"/> and <see cref="Constants"/>; they are kept as read.
/// </remarks>
public sealed class MaterialAnimItem
{
    public string Name { get; set; } = "";

    public ushort ShaderParamCurveIndex { get; set; }

    public ushort TexturePatternCurveIndex { get; set; }

    public ushort VisualConstantIndex { get; set; }

    public ushort VisualCurveIndex { get; set; }

    public ushort BeginVisualConstantIndex { get; set; }

    public List<ParamAnimInfo> ParamAnimInfos { get; set; } = [];

    public List<PatternAnimInfo> PatternAnimInfos { get; set; } = [];

    public List<AnimCurve> Curves { get; set; } = [];

    public List<AnimConstant> Constants { get; set; } = [];

    // The header counts a parameter for each constant and curve of the parameter entries, and each pattern entry once.
    public int ParamCount => ParamAnimInfos.Sum(info => info.ConstantCount + info.FloatCurveCount + info.IntCurveCount);

    public int PatternCount => PatternAnimInfos.Count;
}
