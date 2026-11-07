namespace BfresSharp.Switch.Relocation;

/// <summary>Turns the sorted offsets of a section's pointers into the entries of a relocation table.</summary>
/// <remarks>
/// Takes the lowest pointer not yet covered and the run of consecutive 8-byte pointers from it as one entry. It then
/// looks ahead, past whatever lies between, for the next whole run of the same length within reach of the padding
/// field, and folds that run and every later one at the same distance into the entry as repeats of one structure. A
/// run longer than 255 pointers is split because an entry holds at most 255 offsets.
/// </remarks>
public static class RelocationEncoder
{
    public static List<RelocationEntry> Encode(IReadOnlyList<long> pointers)
    {
        var covered = new bool[pointers.Count];
        var indexOf = new Dictionary<long, int>(pointers.Count);
        for (int index = 0; index < pointers.Count; index++) indexOf[pointers[index]] = index;

        var entries = new List<RelocationEntry>();
        for (int index = 0; index < pointers.Count; index++)
        {
            if (covered[index]) continue;
            entries.Add(EncodeFrom(pointers, indexOf, covered, index));
        }

        return entries;
    }

    private static RelocationEntry EncodeFrom(IReadOnlyList<long> pointers, Dictionary<long, int> indexOf, bool[] covered, int start)
    {
        int run = RunLength(pointers, covered, start);
        Cover(covered, start, run);
        long distance = FindDistance(pointers, covered, start, run);
        if (distance == 0) return new RelocationEntry((uint)pointers[start], 1, run, 0);

        int structures = 1;
        while (structures < RelocationEntry.MaxStructCount && RunAt(pointers, indexOf, covered, pointers[start] + structures * distance, run) is int found)
        {
            Cover(covered, found, run);
            structures++;
        }

        int padding = (int)(distance / RelocationEntry.PointerSize) - run;
        return new RelocationEntry((uint)pointers[start], structures, run, structures > 1 ? padding : 0);
    }

    // The distance to the next uncovered whole run of the same length within reach, or zero when none follows.
    private static long FindDistance(IReadOnlyList<long> pointers, bool[] covered, int start, int run)
    {
        long first = pointers[start];
        long reach = first + (run + RelocationEntry.MaxPaddingCount) * (long)RelocationEntry.PointerSize;
        int next = start + run;
        while (next < pointers.Count && pointers[next] <= reach)
        {
            int length = RunLength(pointers, covered, next);
            if (!covered[next] && length == run) return pointers[next] - first;
            next += length;
        }

        return 0;
    }

    private static int? RunAt(IReadOnlyList<long> pointers, Dictionary<long, int> indexOf, bool[] covered, long position, int run)
    {
        if (!indexOf.TryGetValue(position, out int index) || covered[index]) return null;
        bool continuesAbove = index > 0 && !covered[index - 1] && pointers[index - 1] == position - RelocationEntry.PointerSize;
        return !continuesAbove && RunLength(pointers, covered, index) == run ? index : null;
    }

    private static int RunLength(IReadOnlyList<long> pointers, bool[] covered, int start)
    {
        int length = 1;
        while (start + length < pointers.Count
               && length < RelocationEntry.MaxOffsetCount
               && !covered[start + length]
               && pointers[start + length] == pointers[start] + length * RelocationEntry.PointerSize)
            length++;

        return length;
    }

    private static void Cover(bool[] covered, int start, int count)
    {
        for (int index = 0; index < count; index++) covered[start + index] = true;
    }
}
