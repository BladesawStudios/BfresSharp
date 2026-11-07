namespace BfresSharp.Gx2.Addressing;

/// <summary>What the sizer needs to know about a level: its dimensions in elements, element size and tiling.</summary>
/// <remarks>
/// Cube marks a surface whose slices are cube faces, which pads the slice count differently, and ThreeByte a linear
/// level of a three-byte-per-pixel format, whose pitch is padded in pixels and then tripled.
/// </remarks>
internal readonly record struct Gx2SizeRequest(uint Mode, uint BaseMode, uint Bpp, uint Samples, uint Width, uint Height, uint Slices, uint MipLevel, uint PadDims, bool Cube, bool ThreeByte);

/// <summary>Computes the padded size of a level in a linear, micro tiled or macro tiled mode.</summary>
/// <remarks>
/// Every dimension is rounded up to the alignments of the mode, and levels below the first are first rounded up to a
/// power of two, as the hardware lays them out. A thick macro tiled level that has become too small to fill a macro tile
/// is stored micro tiled instead.
/// </remarks>
internal static class Gx2LevelSizer
{
    public static Gx2LevelInfo Size(in Gx2SizeRequest request)
    {
        if (Gx2TileModes.IsLinear(request.Mode)) return Linear(request);
        return Gx2TileModes.IsMicroTiled(request.Mode) ? MicroTiled(request) : MacroTiled(request);
    }

    private static Gx2LevelInfo Linear(in Gx2SizeRequest r)
    {
        uint thickness = Gx2TileModes.Thickness(r.Mode);
        Gx2Alignments align = Gx2Alignments.Linear(r.Mode, r.Bpp);
        (uint pitch, uint height, uint slices, uint padDims) = Prepare(r, r.PadDims);
        if (r.ThreeByte && r.MipLevel == 0) pitch = Gx2Alignments.NextPow2(r.Width / 3);

        (pitch, height, slices) = Gx2Alignments.Pad(r.Mode, padDims, r.Cube, align.Pitch, align.Height, thickness, pitch, height, slices);
        if (r.ThreeByte && r.MipLevel == 0) pitch *= 3;

        uint size = (height * pitch * (slices * r.Samples / thickness) * r.Bpp * r.Samples + 7) / 8;
        return new Gx2LevelInfo(pitch, height, slices, size, align.Base, align.Pitch, align.Height, thickness, r.Mode, 0);
    }

    private static Gx2LevelInfo MicroTiled(in Gx2SizeRequest r)
    {
        uint mode = r.Mode;
        uint thickness = Gx2TileModes.Thickness(mode);
        (uint pitch, uint height, uint slices, uint padDims) = Prepare(r, r.PadDims);
        if (r.MipLevel != 0 && mode == Gx2TileModes.Micro1dThick && slices < 4)
        {
            mode = Gx2TileModes.Micro1dThin;
            thickness = 1;
        }

        Gx2Alignments align = Gx2Alignments.MicroTiled(mode, r.Bpp, r.Samples);
        (pitch, height, slices) = Gx2Alignments.Pad(mode, padDims, r.Cube, align.Pitch, align.Height, thickness, pitch, height, slices);
        uint size = (height * pitch * slices * r.Bpp * r.Samples + 7) / 8;
        return new Gx2LevelInfo(pitch, height, slices, size, align.Base, align.Pitch, align.Height, thickness, mode, 0);
    }

    private static Gx2LevelInfo MacroTiled(in Gx2SizeRequest r)
    {
        uint mode = r.Mode;
        uint thickness = Gx2TileModes.Thickness(mode);
        (uint pitch, uint height, uint slices, uint padDims) = Prepare(r, r.PadDims);
        if (r.MipLevel != 0 && mode == Gx2TileModes.Macro2dThick && slices < 4)
        {
            mode = Gx2TileModes.Macro2dThin1;
            thickness = 1;
        }

        bool useBase = r.Mode != r.BaseMode && r.MipLevel != 0 && Gx2TileModes.IsThickMacroTiled(r.BaseMode) && !Gx2TileModes.IsThickMacroTiled(r.Mode);
        if (useBase)
        {
            Gx2Alignments baseAlign = Gx2Alignments.MacroTiled(r.BaseMode, r.Bpp, r.Samples);
            uint factor = Math.Max(1, 32 / r.Bpp);
            if (pitch < baseAlign.Pitch * factor || height < baseAlign.Height) return SmallThick(r, baseAlign);
        }

        return Padded(r, mode, thickness, padDims, pitch, height, slices);
    }

    // A level too small for the macro tile of a thick base mode is stored in the thin micro tiled mode.
    private static Gx2LevelInfo SmallThick(in Gx2SizeRequest r, Gx2Alignments baseAlign)
    {
        Gx2LevelInfo micro = MicroTiled(r with { Mode = Gx2TileModes.Micro1dThin });
        return micro with { PitchAlign = baseAlign.Pitch, HeightAlign = baseAlign.Height };
    }

    private static Gx2LevelInfo Padded(in Gx2SizeRequest r, uint mode, uint thickness, uint padDims, uint pitch, uint height, uint slices)
    {
        Gx2Alignments align = Gx2Alignments.MacroTiled(r.Mode, r.Bpp, r.Samples);
        uint pitchAlign = Math.Max(align.Pitch, Gx2Alignments.BankSwappedWidth(r.Mode, r.Bpp, r.Samples, r.Width));
        (pitch, height, slices) = Gx2Alignments.Pad(r.Mode, padDims, r.Cube, pitchAlign, align.Height, thickness, pitch, height, slices);
        uint size = (height * pitch * slices * r.Bpp * r.Samples + 7) / 8;
        return new Gx2LevelInfo(pitch, height, slices, size, align.Base, pitchAlign, align.Height, thickness, mode, 0);
    }

    // Levels below the first are rounded up to powers of two before padding.
    private static (uint Pitch, uint Height, uint Slices, uint PadDims) Prepare(in Gx2SizeRequest r, uint padDims)
    {
        if (r.MipLevel == 0) return (r.Width, r.Height, r.Slices, padDims);
        uint slices = r.Cube ? r.Slices : Gx2Alignments.NextPow2(r.Slices);
        if (r.Cube) padDims = r.Slices <= 1 ? 2u : 0u;
        return (Gx2Alignments.NextPow2(r.Width), Gx2Alignments.NextPow2(r.Height), slices, padDims);
    }
}
