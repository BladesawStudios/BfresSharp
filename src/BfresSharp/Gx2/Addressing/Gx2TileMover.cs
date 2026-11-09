using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BfresSharp.Gx2.Addressing;

/// <summary>Moves the pixels of one slice of a micro or macro tiled level between the surface and plain rows.</summary>
/// <remarks>
/// The level is walked one 8x8 micro tile at a time. A tile that lies wholly inside the picture and the surface is moved
/// with no per-pixel checks, one element of the pixel's own width at a time; a tile on the edge, or pixels of a width
/// that is not 1, 2, 4, 8 or 16 bytes, are moved one element at a time with their bounds checked. Elements that would
/// fall outside the surface are skipped, which leaves the output as it was.
/// </remarks>
internal static class Gx2TileMover
{
    public static void Move(in Gx2LevelInfo level, uint slice, uint swizzle, uint bytes, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        var tiles = new Gx2TileAddresser(level, swizzle, slice);
        switch (bytes)
        {
            case 1: Walk<byte>(tiles, level, bytes, true, surface, linear, toSurface); break;
            case 2: Walk<ushort>(tiles, level, bytes, true, surface, linear, toSurface); break;
            case 4: Walk<uint>(tiles, level, bytes, true, surface, linear, toSurface); break;
            case 8: Walk<ulong>(tiles, level, bytes, true, surface, linear, toSurface); break;
            case 16: Walk<UInt128>(tiles, level, bytes, true, surface, linear, toSurface); break;
            default: Walk<byte>(tiles, level, bytes, false, surface, linear, toSurface); break;
        }
    }

    private static void Walk<T>(Gx2TileAddresser tiles, in Gx2LevelInfo level, uint bytes, bool wholeTiles, Span<byte> surface, Span<byte> linear, bool toSurface)
        where T : unmanaged
    {
        ReadOnlySpan<uint> offsets = tiles.Offsets.Span;
        ReadOnlySpan<uint> spread = tiles.SpreadOffsets.Span;
        uint lastOffset = 0;
        uint lastSpread = 0;
        foreach (uint offset in offsets) lastOffset = Math.Max(lastOffset, offset);
        foreach (uint offset in spread) lastSpread = Math.Max(lastSpread, offset);

        uint wide = level.ElementsWide;
        uint high = level.ElementsHigh;
        for (uint tileY = 0; tileY * 8 < high; tileY++)
        {
            for (uint tileX = 0; tileX * 8 < wide; tileX++)
            {
                (ulong start, uint pipeBank) = tiles.Origin(tileX, tileY);
                uint linearStart = (tileY * 8 * wide + tileX * 8) * bytes;

                // A tile inside the picture whose pixels are all its base plus an offset, and that ends inside the surface, needs no checks.
                if (wholeTiles && wide - tileX * 8 >= 8 && high - tileY * 8 >= 8 && tiles.AddsToBase(start, lastOffset))
                {
                    ulong tileBase = tiles.TileBase(start, pipeBank);
                    if (tileBase + lastSpread + bytes <= (ulong)surface.Length)
                    {
                        WholeTile<T>(tileBase, wide * bytes, linearStart, spread, surface, linear, toSurface);
                        continue;
                    }
                }

                var shape = new TileShape(Math.Min(8, wide - tileX * 8), Math.Min(8, high - tileY * 8), wide * bytes, bytes);
                EdgeTile(tiles, start, pipeBank, shape, linearStart, offsets, surface, linear, toSurface);
            }
        }
    }

    private static void WholeTile<T>(ulong tileBase, uint rowStride, uint linearStart, ReadOnlySpan<uint> spread, Span<byte> surface, Span<byte> linear, bool toSurface)
        where T : unmanaged
    {
        // The caller has checked that the whole tile is inside the surface, and the level that the rows hold the picture.
        ref byte tileInSurface = ref Unsafe.Add(ref MemoryMarshal.GetReference(surface), (nint)tileBase);
        ref byte linearBase = ref MemoryMarshal.GetReference(linear);
        ref uint spreadBase = ref MemoryMarshal.GetReference(spread);
        int size = Unsafe.SizeOf<T>();
        for (int row = 0; row < 8; row++)
        {
            ref byte linearRow = ref Unsafe.Add(ref linearBase, (nint)(linearStart + (uint)row * rowStride));
            for (int column = 0; column < 8; column++)
            {
                ref byte inSurface = ref Unsafe.Add(ref tileInSurface, (nint)Unsafe.Add(ref spreadBase, row * 8 + column));
                ref byte inRows = ref Unsafe.Add(ref linearRow, column * size);
                if (toSurface) Unsafe.WriteUnaligned(ref inSurface, Unsafe.ReadUnaligned<T>(ref inRows));
                else Unsafe.WriteUnaligned(ref inRows, Unsafe.ReadUnaligned<T>(ref inSurface));
            }
        }
    }

    private static void EdgeTile(Gx2TileAddresser tiles, ulong start, uint pipeBank, in TileShape shape, uint linearStart, ReadOnlySpan<uint> offsets, Span<byte> surface, Span<byte> linear, bool toSurface)
    {
        for (uint row = 0; row < shape.Rows; row++)
        {
            for (uint column = 0; column < shape.Columns; column++)
            {
                ulong address = tiles.Address(start, pipeBank, offsets[(int)(row * 8 + column)]);
                if (address + shape.Bytes > (ulong)surface.Length) continue;
                Span<byte> inSurface = surface.Slice((int)address, (int)shape.Bytes);
                Span<byte> inRows = linear.Slice((int)(linearStart + row * shape.RowStride + column * shape.Bytes), (int)shape.Bytes);
                if (toSurface) inRows.CopyTo(inSurface);
                else inSurface.CopyTo(inRows);
            }
        }
    }

    // How much of a tile is inside the picture, and how its pixels sit in the rows.
    private readonly record struct TileShape(uint Columns, uint Rows, uint RowStride, uint Bytes);
}
