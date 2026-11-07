using BfresSharp.Container;
using BfresSharp.Errors;
using BfresSharp.Tests.Corpus;
using BfresSharp.WiiU.IO;
using Xunit.Abstractions;

namespace BfresSharp.Tests.WiiU;

/// <summary>Reads each Wii U file of the corpus, writes it back and compares the bytes.</summary>
/// <remarks>Files that are not readable yet are counted apart. A full run lists the failed files for BFRES_ONLY_FAILED reruns, and a failure can be written out with BFRES_SAVE and BFRES_DIFF_FILE to compare by hand.</remarks>
public sealed class WiiURoundTripTests(ITestOutputHelper output)
{
    private static int FailureLimit => int.TryParse(Environment.GetEnvironmentVariable("BFRES_FAIL_LIMIT"), out int limit) ? limit : 10;

    [CorpusFact]
    public void FilesWithImplementedSectionsWriteBackIdentically()
    {
        int roundTripped = 0, unsupported = 0, failed = 0;
        var failures = new List<string>();
        var failedNames = new List<string>();
        var reasons = new Dictionary<string, int>();
        string wanted = Environment.GetEnvironmentVariable("BFRES_DIFF_FILE") ?? "";
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || header.Platform != BfresPlatform.WiiU) continue;
                byte[] written;
                try
                {
                    written = WiiUFileWriter.Write(WiiUFileReader.Read(data));
                }
                catch (BfresFormatException error)
                {
                    unsupported++;
                    reasons[error.Message] = reasons.GetValueOrDefault(error.Message) + 1;
                    continue;
                }

                if (name == wanted && Environment.GetEnvironmentVariable("BFRES_SAVE") is { } directory)
                {
                    File.WriteAllBytes(Path.Combine(directory, "exp.bin"), data);
                    File.WriteAllBytes(Path.Combine(directory, "got.bin"), written);
                }

                if (data.AsSpan().SequenceEqual(written)) roundTripped++;
                else
                {
                    failed++;
                    failedNames.Add(name);
                    if (failures.Count < FailureLimit) failures.Add($"{name} {header.Version}: {ByteDiff.Describe(data, written)}");
                }
            }
        }

        if (GameFiles.FailedFile is { } list && Environment.GetEnvironmentVariable("BFRES_ONLY_FAILED") is null) File.WriteAllLines(list, failedNames);
        foreach ((string reason, int count) in reasons.OrderByDescending(pair => pair.Value).Take(8)) output.WriteLine($"  {count} x {reason}");
        output.WriteLine($"round tripped {roundTripped}, not yet readable {unsupported}, failed {failed}");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
