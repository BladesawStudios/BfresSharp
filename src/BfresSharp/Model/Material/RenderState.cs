using System.Numerics;
using BfresSharp.Gx2;

namespace BfresSharp.Model.Material;

public enum RenderStateMode : uint
{
    Custom,
    Opaque,
    AlphaMask,
    Translucent,
}

public enum RenderStateBlendMode : uint
{
    None,
    Color,
    Logic,
}

/// <summary>The GX2 render state of a Wii U material: the control registers and the alpha and blend constants.</summary>
/// <remarks>The state is 0x30 bytes in the file. Switch files have no render state.</remarks>
public sealed class RenderState
{
    private const uint ModeMask = 0x3;
    private const uint BlendModeMask = 0x30;

    public uint Flags { get; set; }

    public PolygonControl PolygonControl { get; set; } = new(0)
    {
        FrontFace = GX2FrontFaceMode.CounterClockwise,
        PolygonModeFront = GX2PolygonMode.Triangle,
        PolygonModeBack = GX2PolygonMode.Triangle,
        PolygonModeEnabled = true,
    };

    public DepthControl DepthControl { get; set; } = new(0)
    {
        DepthTestEnabled = true,
        DepthWriteEnabled = true,
        DepthFunc = GX2CompareFunction.LessOrEqual,
        FrontStencilFunc = GX2CompareFunction.Always,
        FrontStencilFail = GX2StencilFunction.Replace,
        FrontStencilZPass = GX2StencilFunction.Replace,
        FrontStencilZFail = GX2StencilFunction.Replace,
        BackStencilFunc = GX2CompareFunction.Always,
        BackStencilFail = GX2StencilFunction.Replace,
        BackStencilZPass = GX2StencilFunction.Replace,
        BackStencilZFail = GX2StencilFunction.Replace,
    };

    public AlphaControl AlphaControl { get; set; } = new(0) { AlphaFunc = GX2CompareFunction.GreaterOrEqual };

    public float AlphaRefValue { get; set; } = 0.5f;

    public ColorControl ColorControl { get; set; } = new(0) { LogicOp = GX2LogicOp.Copy };

    public uint BlendTarget { get; set; }

    public BlendControl BlendControl { get; set; } = new(0)
    {
        ColorSourceBlend = GX2BlendFunction.SourceAlpha,
        ColorDestinationBlend = GX2BlendFunction.OneMinusSourceAlpha,
        ColorCombine = GX2BlendCombine.Add,
        AlphaSourceBlend = GX2BlendFunction.Zero,
        AlphaDestinationBlend = GX2BlendFunction.Zero,
        AlphaCombine = GX2BlendCombine.Add,
        SeparateAlphaBlend = true,
    };

    public Vector4 BlendColor { get; set; }

    public RenderStateMode Mode
    {
        get => (RenderStateMode)(Flags & ModeMask);
        set => Flags = (Flags & ~ModeMask) | (uint)value;
    }

    public RenderStateBlendMode BlendMode
    {
        get => (RenderStateBlendMode)((Flags & BlendModeMask) >> 4);
        set => Flags = (Flags & ~BlendModeMask) | (((uint)value << 4) & BlendModeMask);
    }
}
