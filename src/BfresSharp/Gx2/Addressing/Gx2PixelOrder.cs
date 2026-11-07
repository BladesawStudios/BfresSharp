namespace BfresSharp.Gx2.Addressing;

/// <summary>The order the pixels of one 8x8 micro tile are stored in.</summary>
/// <remarks>
/// A micro tile interleaves the bits of the x and y position of a pixel, in an order that depends on the pixel size,
/// and a thick tile adds the bits of the slice. The table maps a position in the tile (slice, then row, then column) to
/// the byte offset of the pixel inside the tile, so addressing a pixel is one lookup.
/// </remarks>
internal static class Gx2PixelOrder
{
    // For each of the six position bits of a thin tile, which coordinate bit it takes: 0 to 2 are the bits of x and 3 to 5 the bits of y.
    private static ReadOnlySpan<byte> Bits8 => [0, 1, 2, 4, 3, 5];

    private static ReadOnlySpan<byte> Bits16 => [0, 1, 2, 3, 4, 5];

    private static ReadOnlySpan<byte> Bits32 => [0, 1, 3, 2, 4, 5];

    private static ReadOnlySpan<byte> Bits64 => [0, 3, 1, 2, 4, 5];

    private static ReadOnlySpan<byte> Bits128 => [3, 0, 1, 2, 4, 5];

    // The byte offsets inside the tile; micro tiled modes truncate the bit position to a byte and macro tiled modes round it up.
    public static uint[] Offsets(uint bitsPerElement, uint thickness, bool roundUp)
    {
        ReadOnlySpan<byte> order = OrderOf(bitsPerElement);
        var offsets = new uint[64 * thickness];
        for (uint z = 0; z < thickness; z++)
        {
            for (uint y = 0; y < 8; y++)
            {
                for (uint x = 0; x < 8; x++)
                {
                    uint index = Index(order, x, y, z, thickness);
                    offsets[z * 64 + y * 8 + x] = roundUp ? (bitsPerElement * index + 7) / 8 : bitsPerElement * index >> 3;
                }
            }
        }

        return offsets;
    }

    private static ReadOnlySpan<byte> OrderOf(uint bits) => bits switch
    {
        8 => Bits8,
        16 => Bits16,
        64 => Bits64,
        128 => Bits128,
        _ => Bits32,
    };

    private static uint Index(ReadOnlySpan<byte> order, uint x, uint y, uint z, uint thickness)
    {
        uint index = 0;
        for (int bit = 0; bit < 6; bit++)
        {
            uint source = order[bit];
            uint value = source < 3 ? x >> (int)source : y >> (int)(source - 3);
            index |= (value & 1) << bit;
        }

        if (thickness > 1) index |= (z & 1) << 6 | (z >> 1 & 1) << 7;
        if (thickness == 8) index |= (z >> 2 & 1) << 8;
        return index;
    }
}
