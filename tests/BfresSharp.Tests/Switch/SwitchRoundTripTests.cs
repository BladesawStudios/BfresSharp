using BfresSharp.Container;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Switch.IO;
using BfresSharp.Tests.Corpus;
using Xunit.Abstractions;

namespace BfresSharp.Tests.Switch;

public sealed class SwitchRoundTripTests(ITestOutputHelper output)
{
    private static int FailureLimit => int.TryParse(Environment.GetEnvironmentVariable("BFRES_FAIL_LIMIT"), out int limit) ? limit : 10;

    [CorpusFact]
    public void FilesWithImplementedSectionsWriteBackIdentically()
    {
        int roundTripped = 0, unsupported = 0, failed = 0;
        var failures = new List<string>();
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || !header.Platform.UsesSwitchLayout()) continue;
                ResFile file;
                try
                {
                    file = SwitchFileReader.Read(data);
                }
                catch (BfresFormatException)
                {
                    unsupported++;
                    continue;
                }

                string? difference = ByteDiff.Describe(data, SwitchFileWriter.Write(file));
                if (difference is null) roundTripped++;
                else if (++failed > 0 && failures.Count < FailureLimit) failures.Add($"{name} {header.Version}: {difference}");
            }
        }

        output.WriteLine($"round tripped {roundTripped}, not yet readable {unsupported}, failed {failed}");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
