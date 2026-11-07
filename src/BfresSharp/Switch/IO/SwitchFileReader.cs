using BfresSharp.Binary;
using BfresSharp.Container;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.Switch.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.Models;

namespace BfresSharp.Switch.IO;

/// <summary>Reads a whole Switch BFRES file into a <see cref="ResFile"/>.</summary>
/// <remarks>
/// Reads the header, then follows the array and dictionary pointers of every section kind. Nothing is parsed into
/// shared state, so any number of files can be read at once. Sections whose layout is not implemented yet are refused
/// with a <see cref="BfresFormatException"/> naming the section instead of being skipped.
/// </remarks>
public static class SwitchFileReader
{
    public static ResFile Read(ReadOnlyMemory<byte> data)
    {
        BfresHeader header = BfresHeaderReader.Read(data);
        if (!header.Platform.UsesSwitchLayout()) throw new BfresFormatException("Not a Switch file");

        var profile = SwitchProfile.For(header.Version);
        var reader = new SwitchReader(new ByteView(data, ByteOrder.Little), profile);
        var layout = new SwitchRootLayout(profile);
        var view = reader.View;
        var file = new ResFile
        {
            Platform = header.Platform,
            Version = header.Version,
            Alignment = view.U8(0xE),
            TargetAddressSize = view.U8(0xF),
            Flag = view.U16(0x14),
            Name = reader.String(SwitchRootLayout.Name) ?? "",
        };
        long pool = reader.Pointer(layout.StringTable);
        if (pool != 0) file.StringOrder = reader.StringPool(pool, view.U32(layout.StringPoolSize));
        if (profile.Modern)
        {
            file.Reserved = view.Slice(layout.Reserved, SwitchRootLayout.ReservedLength).Span.ToArray();
            file.ExternalFlags = view.U8(layout.Flags);
            file.SavedFlags = view.U8(layout.Flags + 1);
        }

        long bufferInfo = reader.Pointer(layout.BufferInfo);
        if (bufferInfo != 0)
        {
            reader.BufferBase = (long)view.U64(bufferInfo + 8);
            file.BufferInfoKind = view.U32(bufferInfo);
        }
        file.Models = reader.ReadDict(SwitchRootLayout.ModelDict, SwitchRootLayout.Models, ModelCodec.Size(profile), at => ModelCodec.Read(reader, at));
        int counts = layout.AnimCounts;
        file.SkeletalAnims = reader.ReadDict(layout.SkeletalAnims + 8, layout.SkeletalAnims, SkeletalAnimCodec.Size(profile), at => SkeletalAnimCodec.Read(reader, at));
        file.MaterialAnims = reader.ReadDict(layout.MaterialAnims + 8, layout.MaterialAnims, MaterialAnimCodec.Size(profile), at => MaterialAnimCodec.Read(reader, at));
        file.VisibilityAnims = reader.ReadDict(layout.BoneVisibilityAnims + 8, layout.BoneVisibilityAnims, VisibilityAnimCodec.Size(profile), at => VisibilityAnimCodec.Read(reader, at));
        RequireEmpty(reader, layout.ShapeAnims, view.U16(counts + 6), "shape animations");
        RequireEmpty(reader, layout.SceneAnims, view.U16(counts + 8), "scene animations");
        file.ExternalFiles = reader.ReadDict(layout.ExternalFiles + 8, layout.ExternalFiles, ExternalFileCodec.Size, at => ExternalFileCodec.Read(reader, at));
        return file;
    }

    // TODO: Remove as each section's codec is added; the check exists to refuse a file that would otherwise lose a section.
    private static void RequireEmpty(SwitchReader reader, int pointerAt, int count, string what)
    {
        if (count != 0 || reader.Pointer(pointerAt) != 0) throw new BfresFormatException($"Reading {what} is not supported yet", pointerAt);
    }
}
