namespace BfresSharp.Model;

/// <summary>A 3x4 matrix of single-precision floats, stored row by row as the inverse model matrices of a skeleton are.</summary>
public readonly record struct Matrix3x4(
    float M11, float M12, float M13, float M14,
    float M21, float M22, float M23, float M24,
    float M31, float M32, float M33, float M34)
{
    public const int FloatCount = 12;

    public static Matrix3x4 Identity { get; } = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0);

    public float[] ToArray() => [M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34];

    public static Matrix3x4 FromArray(ReadOnlySpan<float> v) => new(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], v[11]);
}
