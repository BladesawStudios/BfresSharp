using BfresSharp.Gx2;
using BfresSharp.Gx2.Addressing;

namespace BfresSharp.Model.Texture;

/// <summary>Reads and writes the pixels of a Wii U texture level by level, as plain rows.</summary>
/// <remarks>
/// Level 0 lives in the texture's image data and every other level in its mip data, at the offsets the texture states
/// (or, for a texture built in code that states none, where the console would put them). A level whose data is not in
/// this file, because a companion file holds it, has no surface and cannot be read or written. Rows hold the elements
/// of the level with no padding: pixels, or 4x4 blocks for block compressed formats.
/// </remarks>
public static class TextureLevels
{
    public static Gx2LevelInfo Info(Model.Texture.Texture texture, int level)
    {
        return Gx2SurfaceLayout.GetLevel(texture.Format, texture.Width, texture.Height, texture.Depth, texture.Dim, texture.TileMode, (uint)texture.AAMode, level);
    }

    // The number of independent images in a level: the array length, the faces of a cube or the depth slices of a volume.
    public static int SliceCount(Model.Texture.Texture texture, int level) => (int)Math.Max(1, Info(texture, level).Depth);

    // The stored bytes of a level, or an empty span when this file does not hold them.
    public static ReadOnlySpan<byte> Surface(Model.Texture.Texture texture, int level)
    {
        (byte[]? data, int start, int size) = Locate(texture, level);
        if (data is null || start < 0 || (long)start + size > data.Length) return default;
        return data.AsSpan(start, size);
    }

    public static byte[] Deswizzle(Model.Texture.Texture texture, int level, int slice = 0)
    {
        Gx2LevelInfo info = Info(texture, level);
        ReadOnlySpan<byte> surface = Surface(texture, level);
        if (surface.IsEmpty) throw new InvalidOperationException($"The texture does not hold the data of level {level}.");
        var rows = new byte[Gx2Swizzler.LinearSize(info)];
        Gx2Swizzler.Deswizzle(info, slice, texture.Swizzle, surface, rows);
        return rows;
    }

    public static void Swizzle(Model.Texture.Texture texture, int level, int slice, ReadOnlySpan<byte> rows)
    {
        Gx2LevelInfo info = Info(texture, level);
        (byte[]? data, int start, int size) = Locate(texture, level);
        if (data is null || start < 0 || (long)start + size > data.Length) throw new InvalidOperationException($"The texture does not hold the data of level {level}.");
        Gx2Swizzler.Swizzle(info, slice, texture.Swizzle, rows, data.AsSpan(start, size));
    }

    private static (byte[]? Data, int Start, int Size) Locate(Model.Texture.Texture texture, int level)
    {
        if ((uint)level >= Math.Max(1, texture.MipCount)) return (null, -1, 0);
        uint size = Info(texture, level).Size;
        if (level == 0) return (texture.Data, 0, (int)size);
        return (texture.MipData, (int)MipStart(texture, level), (int)size);
    }

    // Level 1 starts the mip data, and the rest are where the texture says or, when it says nothing, where the layout puts them.
    private static uint MipStart(Model.Texture.Texture texture, int level)
    {
        if (level == 1) return 0;
        if (texture.MipOffsets.Length >= level && texture.MipOffsets[0] != 0) return texture.MipOffsets[level - 1];
        Gx2MipChain chain = Gx2MipChain.Compute(texture.Format, texture.Width, texture.Height, texture.Depth, texture.Dim, texture.TileMode, (uint)texture.AAMode, texture.Swizzle, (int)texture.MipCount);
        return chain.Offsets[level - 1];
    }
}
