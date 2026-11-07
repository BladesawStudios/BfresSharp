using BfresSharp.Model;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Common;

/// <summary>Reads and writes the embedded files of a Switch archive.</summary>
/// <remarks>
/// An entry is a pointer to the data and its length. The data of every file is placed after the memory pool, each
/// aligned to the archive's page size, or to 512 bytes when it is too short to hold a header.
/// </remarks>
public static class ExternalFileCodec
{
    public const int Size = 16;
    private const int SmallFileLength = 3;
    private const int SmallFileAlignment = 256;

    public static ExternalFile Read(SwitchReader reader, long at)
    {
        long data = reader.Pointer(at);
        int length = (int)reader.View.U64(at + 8);
        return new ExternalFile { Data = data == 0 ? [] : reader.View.Slice(data, length).Span.ToArray() };
    }

    // Writes the entries and returns the slot of each data pointer.
    public static int[] WriteEntries(SwitchWriter writer, ResDict<ExternalFile> files)
    {
        var slots = new int[files.Count];
        for (int index = 0; index < files.Count; index++)
        {
            slots[index] = writer.Pointer();
            writer.Out.U64((ulong)files.Values[index].Data.Length);
        }

        return slots;
    }

    // The alignment the relocation table gets after the data: a page, or less after a file too short to hold a header.
    public static int TailAlignment(ResDict<ExternalFile> files, int pageSize)
    {
        if (files.Count == 0) return 8;
        return files.Values[^1].Data.Length <= SmallFileLength ? SmallFileAlignment : pageSize;
    }

    // Places each file's data at the end of the archive; returns where the first file starts.
    public static long WriteData(SwitchWriter writer, ResDict<ExternalFile> files, int[] slots, int pageSize)
    {
        long start = 0;
        for (int index = 0; index < files.Count; index++)
        {
            byte[] data = files.Values[index].Data;
            writer.Align(data.Length <= SmallFileLength ? SmallFileAlignment : pageSize);
            if (index == 0) start = writer.Position;
            writer.Resolve(slots[index]);
            writer.Out.Bytes(data);
        }

        return start;
    }
}
