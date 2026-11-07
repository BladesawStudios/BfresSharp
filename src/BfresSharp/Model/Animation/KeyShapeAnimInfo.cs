namespace BfresSharp.Model.Animation;

/// <summary>Ties a key shape of a shape to the curve that animates it.</summary>
/// <remarks>A curve index of -1 means no curve animates the key shape.</remarks>
public sealed class KeyShapeAnimInfo
{
    public string Name { get; set; } = "";

    public sbyte CurveIndex { get; set; }

    public sbyte SubBindIndex { get; set; }
}
