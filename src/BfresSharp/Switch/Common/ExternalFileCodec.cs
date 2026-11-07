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
        return new ExternalFile { Data = data == 0 ? default : reader.View.Slice(data, length).Memory };
    }

    // Writes the entries and returns the slot of each data pointer.
    public static int[] WriteEntries(SwitchWriter writer, ResDict<ExternalFile> files)
    {
        var slots = new int[files.Count];
        for (int index = 0; index < files.Count; index++)
        {
            // A file without bytes has no data to point at; version 9 and later still list the empty pointer.
            slots[index] = files.Values[index].Data.Length == 0 && !writer.Profile.Modern ? writer.Unlisted() : writer.Pointer();
            writer.Out.U64((ulong)files.Values[index].Data.Length);
        }

        return slots;
    }

    // The alignment the relocation table gets after the data: a page, or less after a file too short to hold a header.
    public static int TailAlignment(ResDict<ExternalFile> files, int pageSize)
    {
        ExternalFile? last = files.Values.LastOrDefault(file => file.Data.Length > 0);
        if (last is null) return 8;
        return last.Data.Length <= SmallFileLength ? SmallFileAlignment : pageSize;
    }

    // Places each file's data at the end of the archive; returns where the first file starts.
    public static long WriteData(SwitchWriter writer, ResDict<ExternalFile> files, int[] slots, int pageSize)
    {
        long start = 0;
        for (int index = 0; index < files.Count; index++)
        {
            ReadOnlyMemory<byte> data = files.Values[index].Data;
            if (data.Length == 0) continue;
            writer.Align(data.Length <= SmallFileLength ? SmallFileAlignment : pageSize);
            if (start == 0) start = writer.Position;
            writer.Resolve(slots[index]);
            writer.Out.Bytes(data.Span);
        }

        return start;
    }
}
