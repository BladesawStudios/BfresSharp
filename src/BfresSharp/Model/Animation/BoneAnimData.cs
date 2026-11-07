using System.Numerics;

namespace BfresSharp.Model.Animation;

/// <summary>The base transform of a bone animation that its curves are applied to.</summary>
/// <remarks>Only the parts the animation's base flags name are stored in a file; the others stay zero.</remarks>
public sealed class BoneAnimData
{
    public Vector3 Scale { get; set; }

    public Vector4 Rotate { get; set; }

    public Vector3 Translate { get; set; }
}
