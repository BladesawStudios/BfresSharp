namespace BfresSharp.Model.Animation;

/// <summary>How one shader parameter of a material is animated: the curves and constants that drive its components.</summary>
/// <remarks>
/// <see cref="BeginCurve"/> and <see cref="BeginConstant"/> index the lists of the <see cref="MaterialAnimItem"/> that
/// holds this entry. <see cref="SubBindIndex"/> locates the parameter inside the material's parameter data.
/// </remarks>
public sealed class ParamAnimInfo
{
    public string Name { get; set; } = "";

    public ushort BeginCurve { get; set; }

    public ushort FloatCurveCount { get; set; }

    public ushort IntCurveCount { get; set; }

    public ushort BeginConstant { get; set; }

    public ushort ConstantCount { get; set; }

    public ushort SubBindIndex { get; set; }
}
