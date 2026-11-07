namespace BfresSharp.Model.Skeleton;

[Flags]
public enum BoneFlags : uint
{
    Visible = 1 << 0,
}

public enum BoneFlagsRotation : uint
{
    Quaternion = 0,
    EulerXYZ = 1 << 12,
}

public enum BoneFlagsBillboard : uint
{
    None = 0,
    Child = 1 << 16,
    WorldViewVector = 2 << 16,
    WorldViewPoint = 3 << 16,
    ScreenViewVector = 4 << 16,
    ScreenViewPoint = 5 << 16,
    YAxisViewVector = 6 << 16,
    YAxisViewPoint = 7 << 16,
}

[Flags]
public enum BoneFlagsTransform : uint
{
    None = 0,
    ScaleUniform = 1 << 24,
    ScaleVolumeOne = 1 << 25,
    RotateZero = 1 << 26,
    TranslateZero = 1 << 27,
    ScaleOne = ScaleUniform | ScaleVolumeOne,
    RotateTranslateZero = RotateZero | TranslateZero,
    Identity = ScaleOne | RotateZero | TranslateZero,
}

[Flags]
public enum BoneFlagsTransformCumulative : uint
{
    None = 0,
    ScaleUniform = 1 << 28,
    ScaleVolumeOne = 1 << 29,
    RotateZero = 1 << 30,
    TranslateZero = 1u << 31,
    ScaleOne = ScaleVolumeOne | ScaleUniform,
    RotateTranslateZero = RotateZero | TranslateZero,
    Identity = ScaleOne | RotateZero | TranslateZero,
}

public enum SkeletonFlagsScaling : uint
{
    None = 0,
    Standard = 1 << 8,
    Maya = 2 << 8,
    Softimage = 3 << 8,
}

public enum SkeletonFlagsRotation : uint
{
    Quaternion = 0,
    EulerXYZ = 1 << 12,
}
