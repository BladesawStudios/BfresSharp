using BfresSharp.Model;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Common;

/// <summary>Reads and writes embedded files in the Wii U layout, which is 8 bytes.</summary>
/// <remarks>The offset of the data and its size. The data is written behind the name pool; a file of up to three bytes is aligned to 512.</remarks>
public static class ExternalFileCodec
{
    private const int DataAlignment = 0x100;
    private const int FileRank = 9;

    public static ExternalFile Read(WiiUReader reader, long at)
    {
        long data = reader.Offset(at);
        return new ExternalFile { Data = data == 0 ? [] : reader.Bytes(data, reader.View.U32(at + 4)), DataPosition = data == 0 ? -1 : (int)data };
    }

    public static void Write(WiiUWriter writer, ExternalFile file)
    {
        writer.Block(file.Data, new Layout(DataAlignment, FileRank, file.DataPosition, DataAlignment));
        writer.Out.U32((uint)file.Data.Length);
    }
}
