using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Tests.Corpus;
using ZsDicSharp;

namespace BfresSharp.Tests.Switch;

public sealed class ExternalStringTests
{
    [Fact]
    public void AFileOfExternalStringsCannotBeWrittenYet()
    {
        var file = new ResFile();
        file.ExternalStringHashes.Add("ns_param_color0", 0x68E131E72BCD91AD);

        Assert.Throws<BfresUnsupportedException>(() => BfresFile.Write(file));
    }

    [Fact]
    public void NamesByHashTurnsAHashBackIntoItsName()
    {
        var file = new ResFile();
        file.ExternalStringHashes.Add("ns_param_color0", 0x68E131E72BCD91AD);

        Assert.Equal("ns_param_color0", file.NamesByHash()[0x68E131E72BCD91AD]);
    }

    [CorpusFact]
    public void TheExternalBinaryStringFileOfTheCorpusGivesItsNames()
    {
        int checkedCount = 0;
        foreach (string folder in GameFiles.Folders)
        {
            string romfs = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder))!;
            string path = Path.Combine(romfs, "Shader", "ExternalBinaryString.bfres.zs");
            if (!File.Exists(path)) continue;
            using ZsDic zs = ZsDic.FromRomfs(romfs);

            ResFile file = BfresFile.Read(zs.Decompress(File.ReadAllBytes(path), path));

            Assert.True(file.ExternalStringHashes.Count > 0);
            Assert.Empty(file.ExternalFiles);
            Assert.Equal("ns_param_color0", file.NamesByHash()[0x68E131E72BCD91AD]);
            checkedCount++;
        }

        Assert.True(checkedCount > 0, "no Shader/ExternalBinaryString.bfres.zs beside the corpus folders");
    }
}
