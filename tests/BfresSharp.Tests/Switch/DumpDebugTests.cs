using BfresSharp.Binary;
using BfresSharp.Switch.IO;
using BfresSharp.Switch.Relocation;
using BfresSharp.Tests.Corpus;

namespace BfresSharp.Tests.Switch;

public sealed class DumpDebugTests
{
    [Fact]
    public void DumpsOneFile()
    {
        string? needle = Environment.GetEnvironmentVariable("BFRES_DUMP");
        string? outDir = Environment.GetEnvironmentVariable("BFRES_DUMP_DIR");
        if (needle is null || outDir is null) return;
        foreach (string folder in GameFiles.Folders)
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!name.Contains(needle)) continue;
                byte[] actual = SwitchFileWriter.Write(SwitchFileReader.Read(data));
                File.WriteAllBytes(Path.Combine(outDir, "expected.bin"), data);
                File.WriteAllBytes(Path.Combine(outDir, "actual.bin"), actual);
                File.WriteAllLines(Path.Combine(outDir, "expected.ptr"), Pointers(data));
                File.WriteAllLines(Path.Combine(outDir, "actual.ptr"), Pointers(actual));
                return;
            }
    }

    private static IEnumerable<string> Pointers(byte[] data)
    {
        var view = new ByteView(data, ByteOrder.Little);
        RelocationTable table = RelocationTable.Read(view, view.U32(0x18));
        return table.Sections[0].Pointers().Select(p => p.ToString("X"));
    }
}
