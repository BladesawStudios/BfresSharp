using BfresSharp.Tests.Corpus;
using Xunit.Abstractions;

namespace BfresSharp.Tests;

/// <summary>Reads every MeshCodec package of the corpus through the platform-independent entry point.</summary>
public sealed class BfresFileTests(ITestOutputHelper output)
{
    [CorpusFact]
    public void PackagesUnpackAndRead()
    {
        int read = 0, unsupported = 0;
        var failures = new List<string>();
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresFile.IsPackage(data)) continue;
                try
                {
                    Assert.NotNull(BfresFile.Read(data));
                    read++;
                }
                catch (Errors.BfresUnsupportedException)
                {
                    unsupported++;
                }
                catch (Errors.BfresFormatException error)
                {
                    failures.Add($"{name}: {error.Message}");
                }
            }
        }

        output.WriteLine($"read {read} packages, {unsupported} of a MeshCodec version McSharp does not decode, {failures.Count} failed");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(10)));
    }
}
