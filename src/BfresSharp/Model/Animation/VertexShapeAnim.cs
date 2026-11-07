namespace BfresSharp.Model.Animation;

/// <summary>The animation of one shape's key shapes.</summary>
/// <remarks>
/// <see cref="BaseData"/> holds the starting weight of every key shape but the base shape. <see cref="BeginCurve"/> and
/// <see cref="BeginKeyShapeAnim"/> count the curves and infos of the vertex shape animations before this one.
/// </remarks>
public sealed class VertexShapeAnim
{
    public string Name { get; set; } = "";

    public int BeginCurve { get; set; }

    public int BeginKeyShapeAnim { get; set; }

    public List<KeyShapeAnimInfo> KeyShapeAnimInfos { get; set; } = [];

    public List<AnimCurve> Curves { get; set; } = [];

    public float[] BaseData { get; set; } = [];
}
