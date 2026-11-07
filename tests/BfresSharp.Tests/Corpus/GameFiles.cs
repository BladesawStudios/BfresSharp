using Yaz0Sharp;
using ZsDicSharp;

namespace BfresSharp.Tests.Corpus;

/// <summary>Finds and decompresses the BFRES files of the model folders named for the corpus tests.</summary>
/// <remarks>
/// BFRES_CORPUS holds model folders separated by semicolons: the Wii U, Switch and Switch 2 Model directories of
/// the games under test. Unset or missing folders yield no files, and the corpus facts skip themselves. Files are
/// returned decompressed: Yaz0 for BotW, zstd with the dictionaries of the nearest ancestor romfs that has them for
/// the Switch games that compress.
/// </remarks>
internal static class GameFiles
{
    internal const string Env = "BFRES_CORPUS";

    // BFRES_CORPUS_STRIDE=n keeps every n-th file, for quick runs.
    internal static int Stride => int.TryParse(Environment.GetEnvironmentVariable("BFRES_CORPUS_STRIDE"), out int n) && n > 1 ? n : 1;

    // BFRES_FAILED_FILE names a list of relative paths that earlier runs failed; with BFRES_ONLY_FAILED set, only those are read.
    internal static string? FailedFile => Environment.GetEnvironmentVariable("BFRES_FAILED_FILE");

    private static HashSet<string>? OnlyFailed()
    {
        string? path = FailedFile;
        if (path is null || Environment.GetEnvironmentVariable("BFRES_ONLY_FAILED") is null || !File.Exists(path)) return null;
        return File.ReadAllLines(path).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal static IReadOnlyList<string> Folders
    {
        get
        {
            string value = Environment.GetEnvironmentVariable(Env) ?? "";
            return value.Split(';', StringSplitOptions.RemoveEmptyEntries).Where(Directory.Exists).ToList();
        }
    }

    internal static IEnumerable<(string Name, byte[] Data)> BfresFiles(string folder)
    {
        using ZsDic? zs = FindRomfs(folder) is { } romfs ? ZsDic.FromRomfs(romfs) : null;
        int stride = Stride;
        HashSet<string>? only = OnlyFailed();
        foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(IsBfresName).Where((_, index) => index % stride == 0).Where(path => only is null || only.Contains(Path.GetRelativePath(folder, path))))
        {
            byte[] data = File.ReadAllBytes(path);
            if (ZsDic.IsCompressed(data)) data = zs?.Decompress(data, path) ?? data;
            yield return (Path.GetRelativePath(folder, path), Yaz0.DecompressIfNeeded(data));
        }
    }

    private static string? FindRomfs(string folder)
    {
        for (DirectoryInfo? dir = new(folder); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Pack", "ZsDic.pack.zs"))) return dir.FullName;
        }

        return null;
    }

    private static bool IsBfresName(string path)
    {
        string name = Path.GetFileName(path);
        return name.Contains(".bfres", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".sbfres", StringComparison.OrdinalIgnoreCase);
    }
}
