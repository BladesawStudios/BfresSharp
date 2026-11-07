using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BfresSharp.Errors;

namespace BfresSharp.Gx2.Addressing;

/// <summary>Moves the pixels of a slice of a GX2 level between the tiled layout of the console and plain rows.</summary>
/// <remarks>
/// Deswizzle reads a slice out of the stored surface into rows of ElementsWide elements, and Swizzle writes rows into a
/// surface. The rows have no padding. Both walk the surface one 8x8 micro tile at a time, so the work that depends on
/// the tile is done once for 64 pixels. Elements that would fall outside the surface are skipped, which leaves the
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
        else MoveTiled(level, (uint)slice, swizzle, bytes, surface, linear, toSurface);
    }

    private static void MoveLinear(in Gx2LevelInfo level, uint slice, uint bytes, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        ulong sliceStart = (ulong)level.Pitch * level.Height * slice;
        for (uint y = 0; y < level.ElementsHigh; y++)
        {
            for (uint x = 0; x < level.ElementsWide; x++)
            {
                ulong address = (y * (ulong)level.Pitch + x + sliceStart) * bytes;
                Copy(surface, address, linear, (y * level.ElementsWide + x) * bytes, bytes, toSurface);
            }
        }
    }

    private static void MoveTiled(in Gx2LevelInfo level, uint slice, uint swizzle, uint bytes, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        var tiles = new Gx2TileAddresser(level, swizzle, slice);
        ReadOnlySpan<uint> offsets = tiles.Offsets.Span;
        uint wide = level.ElementsWide;
        uint high = level.ElementsHigh;
        for (uint tileY = 0; tileY * 8 < high; tileY++)
        {
            for (uint tileX = 0; tileX * 8 < wide; tileX++)
            {
                (ulong start, uint pipeBank) = tiles.Origin(tileX, tileY);
                uint columns = Math.Min(8, wide - tileX * 8);
                uint rows = Math.Min(8, high - tileY * 8);
                for (uint row = 0; row < rows; row++)
                {
                    uint linearRow = ((tileY * 8 + row) * wide + tileX * 8) * bytes;
                    for (uint column = 0; column < columns; column++)
                    {
                        ulong address = tiles.Address(start, pipeBank, offsets[(int)(row * 8 + column)]);
                        Copy(surface, address, linear, linearRow + column * bytes, bytes, toSurface);
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Copy(Span<byte> surface, ulong address, Span<byte> linear, uint linearOffset, uint bytes, bool toSurface)
    {
        if (address + bytes > (ulong)surface.Length) return;
        ref byte surfaceByte = ref Unsafe.Add(ref MemoryMarshal.GetReference(surface), (nint)address);
        ref byte linearByte = ref Unsafe.Add(ref MemoryMarshal.GetReference(linear), (nint)linearOffset);
        if (toSurface) CopyElement(ref linearByte, ref surfaceByte, bytes);
        else CopyElement(ref surfaceByte, ref linearByte, bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyElement(ref byte from, ref byte to, uint bytes)
    {
        switch (bytes)
        {
            case 1:
                to = from;
                break;
            case 2:
                Unsafe.WriteUnaligned(ref to, Unsafe.ReadUnaligned<ushort>(ref from));
                break;
            case 4:
                Unsafe.WriteUnaligned(ref to, Unsafe.ReadUnaligned<uint>(ref from));
                break;
            case 8:
                Unsafe.WriteUnaligned(ref to, Unsafe.ReadUnaligned<ulong>(ref from));
                break;
            case 16:
                Unsafe.WriteUnaligned(ref to, Unsafe.ReadUnaligned<UInt128>(ref from));
                break;
            default:
                MemoryMarshal.CreateReadOnlySpan(ref from, (int)bytes).CopyTo(MemoryMarshal.CreateSpan(ref to, (int)bytes));
                break;
        }
    }
}
