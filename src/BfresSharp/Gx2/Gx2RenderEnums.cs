#pragma warning disable 1591

namespace BfresSharp.Gx2;

/// <summary>
/// Represents the AA modes (number of samples) for a surface.
/// </summary>
public enum GX2AAMode : uint
{
    Mode1X,
    Mode2X,
    Mode4X,
    Mode8X
}

/// <summary>
/// Represents how the terms of the blend function are combined.
/// </summary>
public enum GX2BlendCombine : uint
{
    Add,
    SourceMinusDestination,
    Minimum,
    Maximum,
    DestinationMinusSource
}

/// <summary>
/// Represents the factors used in the blend function.
/// </summary>
public enum GX2BlendFunction : uint
{
    Zero = 0,
    One = 1,
    SourceColor = 2,
    OneMinusSourceColor = 3,
    SourceAlpha = 4,
    OneMinusSourceAlpha = 5,
    DestinationAlpha = 6,
    OneMinusDestinationAlpha = 7,
    DestinationColor = 8,
    OneMinusDestinationColor = 9,
    SourceAlphaSaturate = 10,
    ConstantColor = 13,
    OneMinusConstantColor = 14,
    Source1Color = 15,
    OneMinusSource1Color = 16,
    Source1Alpha = 17,
    OneMinusSource1Alpha = 18,
    ConstantAlpha = 19,
    OneMinusConstantAlpha = 20,
}

/// <summary>
/// Represents compare functions used for depth and stencil tests.
/// </summary>
public enum GX2CompareFunction : uint
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always
}

/// <summary>
/// Represents the vertex order of front-facing polygons.
/// </summary>
public enum GX2FrontFaceMode : uint
{
    CounterClockwise,
    Clockwise
}

/// <summary>
/// Represents the logic op function to perform.
/// </summary>
public enum GX2LogicOp : uint
{
    /// <summary>
    /// Black
    /// </summary>
    Clear = 0x00,
    /// <summary>
    /// White
    /// </summary>
    Set = 0xFF,
    /// <summary>
    /// Source (Default)
    /// </summary>
    Copy = 0xCC,
    /// <summary>
    /// ~Source
    /// </summary>
    InverseCopy = 0x33,
    /// <summary>
    /// Destination
    /// </summary>
    NoOperation = 0xAA,
    /// <summary>
    /// ~Destination
    /// </summary>
    Inverse = 0x55,
    /// <summary>
    /// Source &amp; Destination
    /// </summary>
    And = 0x88,
    /// <summary>
    /// ~(Source &amp; Destination)
    /// </summary>
    NAnd = 0x77,
    /// <summary>
    /// Source | Destination
    /// </summary>
    Or = 0xEE,
    /// <summary>
    /// ~(Source | Destination)
    /// </summary>
    NOr = 0x11,
    /// <summary>
    /// Source ^ Destination
    /// </summary>
    XOr = 0x66,
    /// <summary>
    ///  ~(Source ^ Destination)
    /// </summary>
    Equivalent = 0x99,
    /// <summary>
    /// Source &amp; ~Destination
    /// </summary>
    ReverseAnd = 0x44,
    /// <summary>
    /// ~Source &amp; Destination
    /// </summary>
    InverseAnd = 0x22,
    /// <summary>
    /// Source | ~Destination
    /// </summary>
    ReverseOr = 0xDD,
    /// <summary>
    /// ~Source | Destination
    /// </summary>
    InverseOr = 0xBB,
}

/// <summary>
/// Represents the base primitive used to draw each side of the polygon when dual-sided polygon mode is enabled.
/// </summary>
public enum GX2PolygonMode : uint
{
    Point,
    Line,
    Triangle
}

/// <summary>
/// Represents the stencil function to be performed if stencil tests pass.
/// </summary>
public enum GX2StencilFunction : uint
{
    Keep,
    Zero,
    Replace,
    Increment,
    Decrement,
    Invert,
    IncrementWrap,
    DecrementWrap
}
