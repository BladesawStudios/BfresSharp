using BfresSharp.Binary;
using BfresSharp.Container;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.WiiU.Animation;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.Models;
using BfresSharp.WiiU.Texture;

namespace BfresSharp.WiiU.IO;

/// <summary>Reads a Wii U BFRES file into a <see cref="ResFile"/>.</summary>
/// <remarks>
/// The header is 108 bytes: the magic, version, byte order mark and header size, the file size and alignment, the
/// name, the size and offset of the name pool, twelve dictionary offsets (models, textures, the four kinds of material
/// animation after the skeletal ones, bone and material visibility, shapes, scenes and embedded files), the counts of
/// each and a user pointer.
/// </remarks>
public static class WiiUFileReader
{
    private const uint MinVersion = 0x02040000;
    private const int HeaderSize = 0x6C;
    private const int DictBase = 0x20;

    public static ResFile Read(ReadOnlyMemory<byte> data)
    {
        BfresHeader header = BfresHeaderReader.Read(data);
        if (header.Platform != BfresPlatform.WiiU || header.ByteOrder != ByteOrder.Big)
            throw new BfresFormatException("Not a big-endian Wii U file");
        if (header.Version.Raw < MinVersion || data.Length < HeaderSize)
            throw new BfresUnsupportedException($"Wii U version {header.Version} is not supported");

        var reader = new WiiUReader(new ByteView(data, ByteOrder.Big), header.Version.Raw);
        var file = new ResFile
        {
            Platform = BfresPlatform.WiiU,
            Version = header.Version,
            DataAlignment = header.DataAlignment,
            Name = reader.String(0x14) ?? "",
        };
        file.Models = reader.ReadDict(DictBase, at => ModelCodec.Read(reader, at));
        file.Textures = reader.ReadDict(DictBase + 4, at => TextureCodec.Read(reader, at));
        file.ExternalFiles = reader.ReadDict(DictBase + 0x2C, at => ExternalFileCodec.Read(reader, at));
        file.SkeletalAnims = reader.ReadDict(DictBase + 8, at => SkeletalAnimCodec.Read(reader, at));
        file.ShaderParamAnims = reader.ReadDict(DictBase + 0xC, at => MaterialAnimCodec.ReadParam(reader, at));
        file.ColorAnims = reader.ReadDict(DictBase + 0x10, at => MaterialAnimCodec.ReadParam(reader, at));
        file.TexSrtAnims = reader.ReadDict(DictBase + 0x14, at => MaterialAnimCodec.ReadParam(reader, at));
        file.TexPatternAnims = reader.ReadDict(DictBase + 0x18, at => MaterialAnimCodec.ReadPattern(reader, at));
        file.VisibilityAnims = reader.ReadDict(DictBase + 0x1C, at => VisibilityAnimCodec.Read(reader, at));
        file.MatVisibilityAnims = reader.ReadDict(DictBase + 0x20, at => VisibilityAnimCodec.Read(reader, at));
        RequireEmpty(reader, DictBase + 0x24, "shape animations");
        RequireEmpty(reader, DictBase + 0x28, "scene animations");
        return file;
    }

    // TODO: Remove as each section's codec is added; the check exists to refuse a file that would otherwise lose a section.
    private static void RequireEmpty(WiiUReader reader, int offsetAt, string what)
    {
        if (reader.Offset(offsetAt) != 0) throw new BfresUnsupportedException($"Reading {what} is not supported yet", offsetAt);
    }
}
