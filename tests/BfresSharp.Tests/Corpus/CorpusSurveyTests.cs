using BfresSharp.Container;
using Xunit.Abstractions;

namespace BfresSharp.Tests.Corpus;

public sealed class CorpusSurveyTests(ITestOutputHelper output)
{
    [CorpusFact]
    public void ListsPlatformsAndVersionsInTheCorpus()
    {
        foreach (string folder in GameFiles.Folders)
        {
            var counts = new SortedDictionary<string, int>();
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                string key = BfresHeaderReader.TryRead(data, out BfresHeader header)
                    ? $"{header.Platform} {header.Version} align {header.DataAlignment}"
                    : "not a bfres: " + Convert.ToHexString(data.AsSpan(0, Math.Min(4, data.Length)));
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }

            output.WriteLine(folder);
            foreach (var pair in counts) output.WriteLine($"  {pair.Value,6}  {pair.Key}");
        }
    }
}
