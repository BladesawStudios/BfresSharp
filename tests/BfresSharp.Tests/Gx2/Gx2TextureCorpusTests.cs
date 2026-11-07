using BfresSharp.Container;
using BfresSharp.Gx2.Addressing;
using BfresSharp.Model.Texture;
using BfresSharp.Tests.Corpus;
using BfresSharp.WiiU.IO;
using Xunit.Abstractions;

namespace BfresSharp.Tests.Gx2;

/// <summary>Checks the surface layout and swizzling against the textures stored in Wii U files.</summary>
public sealed class Gx2TextureCorpusTests(ITestOutputHelper output)
{
    [CorpusFact]
    public void Stored_sizes_and_offsets_match_the_layout()
    {
        int textures = 0, sizeMismatch = 0, offsetMismatch = 0, offsets = 0;
        foreach ((string name, Texture texture) in WiiUTextures())
        {
            textures++;
            if (TextureLevels.Info(texture, 0).Size != texture.DataSize) { sizeMismatch++; Report(name, texture); }
            if (texture.MipCount < 2) continue;
            Gx2MipChain chain = Gx2MipChain.Compute(texture.Format, texture.Width, texture.Height, texture.Depth, texture.Dim, texture.TileMode, (uint)texture.AAMode, texture.Swizzle, (int)texture.MipCount);
            int count = (int)Math.Min(texture.MipCount, Gx2MipChain.StoredOffsets);
            offsets += count - 1;
            if (chain.MipSize != texture.MipDataSize || !chain.Offsets.AsSpan(0, count - 1).SequenceEqual(texture.MipOffsets.AsSpan(0, count - 1))) { offsetMismatch++; Report(name, texture); }
        }

        output.WriteLine($"{textures} textures, {offsets} mip offsets checked, {sizeMismatch} size mismatches, {offsetMismatch} offset mismatches");
        Assert.True(textures > 0);
        Assert.Equal(0, sizeMismatch + offsetMismatch);
    }

    [CorpusFact]
    public void Levels_survive_deswizzling_and_swizzling()
    {
        int levels = 0, changed = 0;
        foreach ((string name, Texture texture) in WiiUTextures())
        {
            for (int level = 0; level < Math.Max(1, texture.MipCount); level++)
            {
                if (TextureLevels.Surface(texture, level).IsEmpty) continue;
                levels++;
                if (!RoundTrips(texture, level)) { changed++; Report(name, texture); }
            }
        }

        output.WriteLine($"{levels} levels, {changed} changed");
        Assert.True(levels > 0);
        Assert.Equal(0, changed);
    }

    // Reading the rows, writing them to a fresh copy of the texture and reading again gives the same rows.
    private static bool RoundTrips(Texture texture, int level)
    {
        byte[] rows = TextureLevels.Deswizzle(texture, level);
        var copy = new Texture { Format = texture.Format, Width = texture.Width, Height = texture.Height, Depth = texture.Depth, Dim = texture.Dim, TileMode = texture.TileMode, AAMode = texture.AAMode, Swizzle = texture.Swizzle, MipCount = texture.MipCount, MipOffsets = texture.MipOffsets };
        copy.Data = new byte[texture.Data?.Length ?? 0];
        copy.MipData = new byte[texture.MipData?.Length ?? 0];
        TextureLevels.Swizzle(copy, level, 0, rows);
        return rows.AsSpan().SequenceEqual(TextureLevels.Deswizzle(copy, level));
    }

    private void Report(string name, Texture texture) => output.WriteLine($"{name}: {texture.Name} {texture.Dim} {texture.Width}x{texture.Height}x{texture.Depth} mips{texture.MipCount} {texture.Format} {texture.TileMode} swizzle {texture.Swizzle:X}");

    private static IEnumerable<(string Name, Texture Texture)> WiiUTextures()
    {
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                if (!BfresHeaderReader.TryRead(data, out BfresHeader header) || header.Platform != BfresPlatform.WiiU) continue;
                foreach (Texture texture in WiiUFileReader.Read(data).Textures.Values) yield return (name, texture);
            }
        }
    }
}
