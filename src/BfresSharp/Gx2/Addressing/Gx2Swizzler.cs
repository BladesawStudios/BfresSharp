using System.Runtime.InteropServices;
using BfresSharp.Errors;

namespace BfresSharp.Gx2.Addressing;

/// <summary>Moves the pixels of a slice of a GX2 level between the tiled layout of the console and plain rows.</summary>
/// <remarks>
/// Deswizzle reads a slice out of the stored surface into rows of ElementsWide elements, and Swizzle writes rows into a
/// surface. The rows have no padding. Tiled levels are walked one 8x8 micro tile at a time by <see cref="Gx2TileMover"/>, and
/// linear levels a row at a time. Elements that would fall outside the surface are skipped, which leaves the
/// output as it was. The swizzle value of the texture supplies the pipe and bank bits of the first macro tile. Colour
/// surfaces with one sample are handled; anything else raises <see cref="BfresUnsupportedException"/>.
/// </remarks>
public static class Gx2Swizzler
{
    public static void Deswizzle(in Gx2LevelInfo level, int slice, uint swizzle, ReadOnlySpan<byte> surface, Span<byte> linear)
    {
        Move(level, slice, swizzle, MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(surface), surface.Length), linear, false);
    }

    public static void Swizzle(in Gx2LevelInfo level, int slice, uint swizzle, ReadOnlySpan<byte> linear, Span<byte> surface)
    {
        Move(level, slice, swizzle, surface, MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(linear), linear.Length), true);
    }

    public static int LinearSize(in Gx2LevelInfo level) => checked((int)(level.ElementsWide * level.ElementsHigh * (level.BitsPerElement / 8)));

    // The linear span is only written to when toSurface is false, and the surface only when it is true.
    private static void Move(in Gx2LevelInfo level, int slice, uint swizzle, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        if (level.Samples > 1) throw new BfresUnsupportedException("Surfaces with more than one sample are not tiled by this library.");
        if (level.BitsPerElement % 8 != 0 || level.BitsPerElement == 0) throw new BfresUnsupportedException($"Elements of {level.BitsPerElement} bits are not tiled by this library.");
        if ((uint)slice >= Math.Max(1, level.Depth)) throw new ArgumentOutOfRangeException(nameof(slice));
        if (linear.Length < LinearSize(level)) throw new ArgumentException("The row buffer is smaller than the level's rows.", nameof(linear));

        uint bytes = level.BitsPerElement / 8;
        if (Gx2TileModes.IsLinear(level.TileMode)) MoveLinear(level, (uint)slice, bytes, surface, linear, toSurface);
        else Gx2TileMover.Move(level, (uint)slice, swizzle, bytes, surface, linear, toSurface);
    }

    private static void MoveLinear(in Gx2LevelInfo level, uint slice, uint bytes, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        ulong sliceStart = (ulong)level.Pitch * level.Height * slice;
        int rowBytes = checked((int)(level.ElementsWide * bytes));
        for (uint y = 0; y < level.ElementsHigh; y++)
        {
            // A row is contiguous in the surface; the elements past its end are skipped.
            ulong address = (y * (ulong)level.Pitch + sliceStart) * bytes;
            ulong available = address < (ulong)surface.Length ? (ulong)surface.Length - address : 0;
            int count = (int)Math.Min((ulong)rowBytes, available / bytes * bytes);
            if (count == 0) continue;

            Span<byte> inSurface = surface.Slice((int)address, count);
            Span<byte> inRows = linear.Slice((int)(y * (uint)rowBytes), count);
            if (toSurface) inRows.CopyTo(inSurface);
            else inSurface.CopyTo(inRows);
        }
    }
}
