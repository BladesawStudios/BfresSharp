namespace BfresSharp.Model.Animation;

/// <summary>How the frame numbers of a curve are stored.</summary>
/// <remarks>The values are the bits of the curve flags they occupy.</remarks>
public enum AnimCurveFrameType : ushort
{
    Single,
    Decimal10x5,
    Byte,
}

/// <summary>How the key values of a curve are stored.</summary>
/// <remarks>The values are the bits of the curve flags they occupy.</remarks>
public enum AnimCurveKeyType : ushort
{
    Single = 0 << 2,
    Int16 = 1 << 2,
    SByte = 2 << 2,
}

/// <summary>The kind of interpolation a curve uses between keys.</summary>
/// <remarks>The values are the bits of the curve flags they occupy.</remarks>
public enum AnimCurveType : ushort
{
    Cubic = 0 << 4,
    Linear = 1 << 4,
    BakedFloat = 2 << 4,
    StepFloat = 3 << 4,
    StepInt = 4 << 4,
    BakedInt = 5 << 4,
    StepBool = 6 << 4,
    BakedBool = 7 << 4,
}

/// <summary>What a curve does before its first or after its last key.</summary>
/// <remarks>Stored in two bits of the curve flags.</remarks>
public enum WrapMode
{
    Clamp,
    Repeat,
    Mirror,
}
