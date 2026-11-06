namespace BfresSharp.Switch.Relocation;

/// <summary>One run of pointers in a Switch file, as stored in its relocation table.</summary>
/// <remarks>
/// Describes <see cref="StructCount"/> repeats of a structure that starts with <see cref="OffsetCount"/> consecutive
/// 8-byte pointers followed by <see cref="PaddingCount"/> 8-byte words that hold no pointer. The first pointer is at
/// <see cref="Position"/>, an absolute file offset. The runtime adds the load address to every pointer listed.
/// </remarks>
public readonly record struct RelocationEntry(uint Position, int StructCount, int OffsetCount, int PaddingCount)
{
    public const int PointerSize = 8;

    public const int MaxOffsetCount = 255;

    public const int MaxStructCount = ushort.MaxValue;

    public int StrideBytes => (OffsetCount + PaddingCount) * PointerSize;

    public IEnumerable<long> Pointers()
    {
        for (int structure = 0; structure < StructCount; structure++)
        {
            for (int pointer = 0; pointer < OffsetCount; pointer++)
                yield return Position + (long)structure * StrideBytes + pointer * PointerSize;
        }
    }
}
