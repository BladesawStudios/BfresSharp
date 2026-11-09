using System.Runtime.CompilerServices;

namespace BfresSharp.Gx2.Addressing;

/// <summary>Finds where the pixels of one slice of a micro or macro tiled level are stored.</summary>
/// <remarks>
/// Everything that is the same for every pixel of the slice is worked out once. A micro tile of 8x8 pixels then needs
/// one call to <see cref="Origin"/>, which finds the tile's place in the surface, and each pixel in it one call to
/// <see cref="Address"/> with its entry in <see cref="Offsets"/>. Macro tiled modes spread tiles over the pipes and banks
/// of the console's memory; the pipe and bank of a tile become bits 8 to 10 of its address. Most tiles are a base address plus
/// the pixel's <see cref="SpreadOffsets"/> entry, which lets a caller address them without the per-pixel work. The level has one sample.
/// </remarks>
internal sealed class Gx2TileAddresser
{
    // Which bank bits a bank swapped mode flips, by the index of the macro tile along the row.
    private static ReadOnlySpan<byte> BankSwapOrder => [0, 1, 3, 2];

    private readonly bool _macro;
    private readonly uint _microTileBytes;
    private readonly uint _tilesPerRow;
    private readonly uint _macroWidth;
    private readonly uint _macroHeight;
    private readonly uint _macroTileBytes;
    private readonly uint _swapWidth;
    private readonly uint _sliceXor;
    private readonly ulong _sliceOffset;

    public Gx2TileAddresser(in Gx2LevelInfo level, uint swizzle, uint slice)
    {
        uint mode = level.TileMode;
        uint thickness = Gx2TileModes.Thickness(mode);
        uint bits = level.BitsPerElement;
        _macro = !Gx2TileModes.IsMicroTiled(mode);
        Offsets = Gx2PixelOrder.Offsets(bits, thickness, _macro).AsMemory((int)(slice % thickness * 64), 64);
        SpreadOffsets = _macro ? Spread(Offsets.Span) : Offsets;

        if (!_macro)
        {
            _microTileBytes = (64 * thickness * bits + 7) / 8;
            _tilesPerRow = level.Pitch >> 3;
            _sliceOffset = (ulong)(slice / thickness) * ((level.Pitch * level.Height * thickness * bits + 7) / 8);
            return;
        }

        (_macroWidth, _macroHeight) = Gx2TileModes.MacroTileSize(mode);
        _tilesPerRow = level.Pitch / _macroWidth;
        _macroTileBytes = (thickness * bits * _macroHeight * _macroWidth + 7) / 8;
        _swapWidth = Gx2TileModes.IsBankSwapped(mode) ? Gx2Alignments.BankSwappedWidth(mode, bits, 1, level.Pitch) : 0;

        uint sliceIn = Gx2TileModes.IsThickMacroTiled(mode) ? slice >> 2 : slice;
        uint pipeSwizzle = swizzle >> 8 & 1;
        uint bankSwizzle = swizzle >> 9 & 3;
        _sliceXor = pipeSwizzle + 2 * bankSwizzle + sliceIn * Gx2TileModes.Rotation(mode) & 7;
        ulong sliceBytes = ((ulong)level.Height * level.Pitch * thickness * bits + 7) / 8;
        _sliceOffset = sliceBytes * slice / thickness;
    }

    // The byte offsets of the 64 pixels of a tile, indexed by row then column.
    public ReadOnlyMemory<uint> Offsets { get; }

    // The place of the tile at the given tile column and row: an offset to add to a pixel's offset, and the pipe and bank bits.
    public (ulong Start, uint PipeBank) Origin(uint tileX, uint tileY)
    {
        if (!_macro) return (_microTileBytes * ((ulong)tileX + (ulong)tileY * _tilesPerRow) + _sliceOffset, 0);

        uint pipe = (tileY ^ tileX) & 1;
        uint bank = (tileY >> 2 ^ tileX) & 1 | 2 * ((tileY >> 1 ^ tileX >> 1) & 1);
        uint bankPipe = pipe + 2 * bank ^ _sliceXor;
        pipe = bankPipe & 1;
        bank = bankPipe >> 1;

        uint macroX = tileX * 8 / _macroWidth;
        uint macroY = tileY * 8 / _macroHeight;
        ulong macroOffset = ((ulong)macroX + (ulong)_tilesPerRow * macroY) * _macroTileBytes;
        if (_swapWidth != 0) bank ^= BankSwapOrder[(int)(_macroWidth * macroX / _swapWidth & 3)];

        return ((macroOffset + _sliceOffset) >> 3, bank << 9 | pipe << 8);
    }

    // The offsets with the bits above the eighth moved up three, as an address does; added to a tile base they give pixel addresses.
    public ReadOnlyMemory<uint> SpreadOffsets { get; }

    // Whether every pixel of the tile at this start is its spread offset added to TileBase: the tile starts on a multiple of
    // 256, or does not reach past the next multiple.
    public bool AddsToBase(ulong start, uint lastOffset) => !_macro || (start & 255) == 0 || (start & 255) + lastOffset < 256;

    public ulong TileBase(ulong start, uint pipeBank) => _macro ? pipeBank | (start & ~255UL) << 3 | start & 255 : start;

    private static uint[] Spread(ReadOnlySpan<uint> offsets)
    {
        var spread = new uint[offsets.Length];
        for (int i = 0; i < spread.Length; i++) spread[i] = offsets[i] & 255 | (offsets[i] & ~255u) << 3;
        return spread;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Address(ulong start, uint pipeBank, uint offset)
    {
        ulong total = start + offset;
        if (!_macro) return total;
        return pipeBank | total & 255 | (total & ~255UL) << 3;
    }
}
