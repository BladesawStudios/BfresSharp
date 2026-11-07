using BfresSharp.Binary;

namespace BfresSharp.WiiU.IO;

/// <summary>Collects the large data (indices, vertices, texels, embedded files) that a Wii U file keeps behind its name pool.</summary>
/// <remarks>
/// The section starts and ends on a 0x40 boundary. Blocks are written ordered by rank, then by the order they were added:
/// index data first, then vertex data, then texture data, then embedded files. A block added twice with the same key is
/// written once and every offset to it is filled in. A block with a position is written there instead of at the end, which
/// reproduces files whose textures overlap or sit where a companion file would have put them.
/// </remarks>
internal sealed class WiiUDataBlocks
{
    private const int SectionAlignment = 0x40;
    private readonly Dictionary<object, Block> _known = new(ReferenceEqualityComparer.Instance);
    private readonly List<Block> _blocks = [];

    public void Add(object key, byte[] bytes, Layout layout, int field)
    {
        if (!_known.TryGetValue(key, out Block? block))
        {
            block = new Block(bytes, layout);
            _known[key] = block;
            _blocks.Add(block);
        }

        block.Fields.Add(field);
    }

    public void Write(ByteBuilder o)
    {
        if (_blocks.Count > 0) o.Align(SectionAlignment);
        foreach (Block block in _blocks.OrderBy(block => block.Layout.Rank))
        {
            int position = block.Layout.Position;
            if (position < 0)
            {
                if (block.Layout.Alignment != 0) o.Align(block.Layout.Alignment);
                position = o.Length;
            }

            foreach (int field in block.Fields) o.SetU32(field, (uint)(position - field));
            o.SetBytes(position, block.Bytes);
            if (block.Layout.PadTo != 0) o.Align(block.Layout.PadTo);
        }

        if (_blocks.Count > 0) o.Align(SectionAlignment);
    }

    private sealed class Block(byte[] bytes, Layout layout)
    {
        public List<int> Fields { get; } = [];

        public byte[] Bytes { get; } = bytes;

        public Layout Layout { get; } = layout;
    }
}

/// <summary>Where a data block goes: its alignment and rank, a fixed position if it has one, and what the content is padded to.</summary>
public readonly record struct Layout(int Alignment, int Rank, int Position = -1, int PadTo = 0);
