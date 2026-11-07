using BfresSharp.Container;
using BfresSharp.Model;
using BfresSharp.Switch.IO;
using BfresSharp.Tests.Corpus;
using Xunit.Abstractions;

namespace BfresSharp.Tests.Switch;

public sealed class SwitchModelReadTests(ITestOutputHelper output)
{
    [CorpusFact]
    public void ModelsReadWithConsistentCounts()
    {
        int models = 0, shapes = 0, files = 0;
        var failures = new Dictionary<string, int>();
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || !header.Platform.UsesSwitchLayout()) continue;
                try
                {
                    ResFile file = SwitchFileReader.Read(data);
                    files++;
                    models += file.Models.Count;
                    foreach (ResModel model in file.Models.Values)
                    {
                        shapes += model.Shapes.Count;
                        Check(model);
                    }
                }
                catch (Exception exception)
                {
                    string key = $"{header.Version}: {exception.GetType().Name}: {exception.Message}";
                    failures[key] = failures.GetValueOrDefault(key) + 1;
                    if (failures[key] == 1) output.WriteLine($"{name} -> {key}");
                }
            }
        }

        output.WriteLine($"files {files}, models {models}, shapes {shapes}, failures {failures.Values.Sum()}");
        foreach ((string key, int count) in failures.OrderByDescending(pair => pair.Value).Take(10)) output.WriteLine($"{count}x {key}");
        Assert.Empty(failures);
    }

    private static void Check(ResModel model)
    {
        foreach (var shape in model.Shapes.Values)
        {
            var buffer = model.VertexBuffers[shape.VertexBufferIndex];
            Assert.True(buffer.VertexCount > 0 || buffer.Streams.Count == 0);
            foreach (var stream in buffer.Streams) Assert.True(stream.Data.Length >= buffer.VertexCount * stream.Stride);
            foreach (var mesh in shape.Meshes) Assert.True(mesh.IndexData.Length > 0);
        }
    }
}
