namespace BfresSharp.Switch.Relocation;

/// <summary>Turns the sorted offsets of a section's pointers into the entries of a relocation table.</summary>
/// <remarks>
/// Takes each run of consecutive 8-byte pointers as one entry, then folds in the runs that follow it at the same
/// distance with the same length as repeats of one structure. A run longer than 255 pointers is split because an
/// entry holds at most 255 offsets.
/// </remarks>
public static class RelocationEncoder
{
    public static List<RelocationEntry> Encode(IReadOnlyList<long> pointers)
    {
        var entries = new List<RelocationEntry>();
        int index = 0;
        while (index < pointers.Count)
        {
            int run = RunLength(pointers, index);
            int structures = 1;
            int padding = 0;
            int next = index + run;
            if (next < pointers.Count && run < RelocationEntry.MaxOffsetCount)
            {
                long distance = pointers[next] - pointers[index];
                long gap = distance / RelocationEntry.PointerSize - run;
                if (gap <= RelocationEntry.MaxPaddingCount)
                {
                    padding = (int)gap;
                    structures = CountRepeats(pointers, index, run, distance);
                }
            }

            entries.Add(new RelocationEntry((uint)pointers[index], structures, run, structures > 1 ? padding : 0));
            index += run * structures;
        }

        return entries;
    }

    private static int RunLength(IReadOnlyList<long> pointers, int start)
    {
        int length = 1;
        while (start + length < pointers.Count
               && length < RelocationEntry.MaxOffsetCount
               && pointers[start + length] == pointers[start] + length * RelocationEntry.PointerSize)
            length++;

        return length;
    }

    // How many times the run at start repeats at the given distance, counting the run itself.
    private static int CountRepeats(IReadOnlyList<long> pointers, int start, int run, long distance)
    {
        int repeats = 1;
        while (repeats < RelocationEntry.MaxStructCount && RepeatsAt(pointers, start, run, distance, repeats)) repeats++;
        return repeats;
    }

    private static bool RepeatsAt(IReadOnlyList<long> pointers, int start, int run, long distance, int repeat)
    {
        int first = start + repeat * run;
        if (first + run > pointers.Count) return false;
        if (RunLength(pointers, first) != run) return false;
        return pointers[first] == pointers[start] + repeat * distance;
    }
}
