using BfresSharp.Gx2;

namespace BfresSharp.Model.Material;

internal static class Bits
{
    public static bool Get(uint value, int bit) => ((value >> bit) & 1) != 0;

    public static uint Set(uint value, int bit, bool on) => on ? value | (1u << bit) : value & ~(1u << bit);

    public static uint Get(uint value, int bit, int width) => (value >> bit) & ((1u << width) - 1);

    public static uint Set(uint value, int bit, int width, uint field)
    {
        uint mask = ((1u << width) - 1) << bit;
        return (value & ~mask) | ((field << bit) & mask);
    }
}

/// <summary>The GX2 polygon control register: culling, winding and polygon modes.</summary>
public struct PolygonControl(uint value)
{
    public uint Value { get; set; } = value;

    public bool CullFront { readonly get => Bits.Get(Value, 0); set => Value = Bits.Set(Value, 0, value); }

    public bool CullBack { readonly get => Bits.Get(Value, 1); set => Value = Bits.Set(Value, 1, value); }

    public GX2FrontFaceMode FrontFace
    {
        readonly get => Bits.Get(Value, 2) ? GX2FrontFaceMode.Clockwise : GX2FrontFaceMode.CounterClockwise;
        set => Value = Bits.Set(Value, 2, value == GX2FrontFaceMode.Clockwise);
    }

    public bool PolygonModeEnabled { readonly get => Bits.Get(Value, 3); set => Value = Bits.Set(Value, 3, value); }

    public GX2PolygonMode PolygonModeFront
    {
        readonly get => (GX2PolygonMode)Bits.Get(Value, 5, 3);
        set => Value = Bits.Set(Value, 5, 3, (uint)value);
    }

    public GX2PolygonMode PolygonModeBack
    {
        readonly get => (GX2PolygonMode)Bits.Get(Value, 8, 3);
        set => Value = Bits.Set(Value, 8, 3, (uint)value);
    }

    public bool PolygonOffsetFrontEnabled { readonly get => Bits.Get(Value, 11); set => Value = Bits.Set(Value, 11, value); }

    public bool PolygonOffsetBackEnabled { readonly get => Bits.Get(Value, 12); set => Value = Bits.Set(Value, 12, value); }

    public bool PolygonLineOffsetEnabled { readonly get => Bits.Get(Value, 13); set => Value = Bits.Set(Value, 13, value); }
}

/// <summary>The GX2 alpha test control register.</summary>
public struct AlphaControl(uint value)
{
    public uint Value { get; set; } = value;

    public GX2CompareFunction AlphaFunc
    {
        readonly get => (GX2CompareFunction)Bits.Get(Value, 0, 3);
        set => Value = Bits.Set(Value, 0, 3, (uint)value);
    }

    public bool AlphaTestEnabled { readonly get => Bits.Get(Value, 3); set => Value = Bits.Set(Value, 3, value); }
}

/// <summary>The GX2 color control register: write masks and the logic operation.</summary>
public struct ColorControl(uint value)
{
    public uint Value { get; set; } = value;

    public bool MultiWriteEnabled { readonly get => Bits.Get(Value, 1); set => Value = Bits.Set(Value, 1, value); }

    public bool ColorBufferEnabled { readonly get => Bits.Get(Value, 4); set => Value = Bits.Set(Value, 4, value); }

    public byte BlendEnableMask
    {
        readonly get => (byte)Bits.Get(Value, 8, 8);
        set => Value = Bits.Set(Value, 8, 8, value);
    }

    public GX2LogicOp LogicOp
    {
        readonly get => (GX2LogicOp)Bits.Get(Value, 16, 8);
        set => Value = Bits.Set(Value, 16, 8, (uint)value);
    }
}

/// <summary>The GX2 blend control register.</summary>
public struct BlendControl(uint value)
{
    public uint Value { get; set; } = value;

    public GX2BlendFunction ColorSourceBlend
    {
        readonly get => (GX2BlendFunction)Bits.Get(Value, 0, 5);
        set => Value = Bits.Set(Value, 0, 5, (uint)value);
    }

    public GX2BlendCombine ColorCombine
    {
        readonly get => (GX2BlendCombine)Bits.Get(Value, 5, 3);
        set => Value = Bits.Set(Value, 5, 3, (uint)value);
    }

    public GX2BlendFunction ColorDestinationBlend
    {
        readonly get => (GX2BlendFunction)Bits.Get(Value, 8, 5);
        set => Value = Bits.Set(Value, 8, 5, (uint)value);
    }

    public GX2BlendFunction AlphaSourceBlend
    {
        readonly get => (GX2BlendFunction)Bits.Get(Value, 16, 5);
        set => Value = Bits.Set(Value, 16, 5, (uint)value);
    }

    public GX2BlendCombine AlphaCombine
    {
        readonly get => (GX2BlendCombine)Bits.Get(Value, 21, 3);
        set => Value = Bits.Set(Value, 21, 3, (uint)value);
    }

    public GX2BlendFunction AlphaDestinationBlend
    {
        readonly get => (GX2BlendFunction)Bits.Get(Value, 24, 5);
        set => Value = Bits.Set(Value, 24, 5, (uint)value);
    }

    public bool SeparateAlphaBlend { readonly get => Bits.Get(Value, 29); set => Value = Bits.Set(Value, 29, value); }
}
