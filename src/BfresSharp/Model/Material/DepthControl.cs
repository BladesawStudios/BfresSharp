using BfresSharp.Gx2;

namespace BfresSharp.Model.Material;

/// <summary>The GX2 depth and stencil control register.</summary>
public struct DepthControl(uint value)
{
    public uint Value { get; set; } = value;

    public bool StencilTestEnabled { readonly get => Bits.Get(Value, 0); set => Value = Bits.Set(Value, 0, value); }

    public bool DepthTestEnabled { readonly get => Bits.Get(Value, 1); set => Value = Bits.Set(Value, 1, value); }

    public bool DepthWriteEnabled { readonly get => Bits.Get(Value, 2); set => Value = Bits.Set(Value, 2, value); }

    public GX2CompareFunction DepthFunc { readonly get => Compare(4); set => SetField(4, (uint)value); }

    public bool BackStencilEnabled { readonly get => Bits.Get(Value, 7); set => Value = Bits.Set(Value, 7, value); }

    public GX2CompareFunction FrontStencilFunc { readonly get => Compare(8); set => SetField(8, (uint)value); }

    public GX2StencilFunction FrontStencilFail { readonly get => Stencil(11); set => SetField(11, (uint)value); }

    public GX2StencilFunction FrontStencilZPass { readonly get => Stencil(14); set => SetField(14, (uint)value); }

    public GX2StencilFunction FrontStencilZFail { readonly get => Stencil(17); set => SetField(17, (uint)value); }

    public GX2CompareFunction BackStencilFunc { readonly get => Compare(20); set => SetField(20, (uint)value); }

    public GX2StencilFunction BackStencilFail { readonly get => Stencil(23); set => SetField(23, (uint)value); }

    public GX2StencilFunction BackStencilZPass { readonly get => Stencil(26); set => SetField(26, (uint)value); }

    public GX2StencilFunction BackStencilZFail { readonly get => Stencil(29); set => SetField(29, (uint)value); }

    private readonly GX2CompareFunction Compare(int bit) => (GX2CompareFunction)Bits.Get(Value, bit, 3);

    private readonly GX2StencilFunction Stencil(int bit) => (GX2StencilFunction)Bits.Get(Value, bit, 3);

    private void SetField(int bit, uint field) => Value = Bits.Set(Value, bit, 3, field);
}
