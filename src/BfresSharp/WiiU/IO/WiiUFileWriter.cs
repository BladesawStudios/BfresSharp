using BfresSharp.Model;
using BfresSharp.WiiU.Animation;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.Models;
using BfresSharp.WiiU.Texture;

namespace BfresSharp.WiiU.IO;

/// <summary>Writes a <see cref="ResFile"/> in the Wii U layout; <see cref="WiiUFileReader"/> reads it.</summary>
/// <remarks>
/// The header is written first with an offset for each dictionary, each of which joins the queue of the writer. The
/// queue is then drained in order, which places the models, textures and the records they hold, and the name pool and
/// data blocks follow. The file size and the pool position and size are filled in last.
/// </remarks>
public static class WiiUFileWriter
{
    private const int FileSizeAt = 0xC;
    private const int PoolSizeAt = 0x18;

    public static byte[] Write(ResFile file) => Build(file).ToArray();

    // Writes straight from the builder, which saves copying a finished file into an array first.
    public static void Write(ResFile file, Stream destination) => destination.Write(Build(file).Written);

    private static Binary.ByteBuilder Build(ResFile file)
    {
        var writer = new WiiUWriter(file.Version.Raw, (int)file.DataAlignment);
        var o = writer.Out;
        o.Bytes("FRES"u8).U32(file.Version.Raw).U16(0xFEFF).U16(0x10).U32(0).U32(file.DataAlignment);
        writer.Name(file.Name);
        o.U32(0).U32(0);
        writer.Dict(file.Models, (model, _) => ModelCodec.Write(writer, model, file.Textures));
        writer.Dict(file.Textures, (texture, _) => TextureCodec.Write(writer, texture));
        writer.Dict(file.SkeletalAnims, (anim, _) => SkeletalAnimCodec.Write(writer, anim));
        writer.Dict(file.ShaderParamAnims, (anim, _) => MaterialAnimCodec.WriteParam(writer, anim));
        writer.Dict(file.ColorAnims, (anim, _) => MaterialAnimCodec.WriteParam(writer, anim));
        writer.Dict(file.TexSrtAnims, (anim, _) => MaterialAnimCodec.WriteParam(writer, anim));
        writer.Dict(file.TexPatternAnims, (anim, _) => MaterialAnimCodec.WritePattern(writer, anim));
        writer.Dict(file.VisibilityAnims, (anim, _) => VisibilityAnimCodec.Write(writer, anim));
        writer.Dict(file.MatVisibilityAnims, (anim, _) => VisibilityAnimCodec.Write(writer, anim));
        o.Zeros(4 * 2);
        writer.Dict(file.ExternalFiles, (external, _) => ExternalFileCodec.Write(writer, external));
        WriteCounts(o, file);
        o.U32(0);
        writer.PlaceValues(file.ExternalFiles);
        writer.Place(file.Models, file.Textures, file.SkeletalAnims, file.TexPatternAnims, file.ShaderParamAnims, file.ColorAnims, file.TexSrtAnims, file.VisibilityAnims, file.MatVisibilityAnims, file.ExternalFiles);
        writer.PlaceValues(file.Models);
        writer.PlaceValues(file.Textures);
        writer.PlaceValues(file.SkeletalAnims);
        writer.PlaceValues(file.TexPatternAnims);
        writer.PlaceValues(file.ShaderParamAnims);
        writer.PlaceValues(file.ColorAnims);
        writer.PlaceValues(file.TexSrtAnims);
        writer.PlaceValues(file.VisibilityAnims);
        writer.PlaceValues(file.MatVisibilityAnims);
        writer.Finish();
        o.SetU32(FileSizeAt, (uint)o.Length);
        o.SetU32(PoolSizeAt, (uint)writer.Pool.Size);
        o.SetU32(PoolSizeAt + 4, (uint)(writer.Pool.Start - (PoolSizeAt + 4)));
        return o;
    }

    private static void WriteCounts(Binary.ByteBuilder o, ResFile file)
    {
        o.U16((ushort)file.Models.Count).U16((ushort)file.Textures.Count).U16((ushort)file.SkeletalAnims.Count)
            .U16((ushort)file.ShaderParamAnims.Count).U16((ushort)file.ColorAnims.Count).U16((ushort)file.TexSrtAnims.Count)
            .U16((ushort)file.TexPatternAnims.Count).U16((ushort)file.VisibilityAnims.Count).U16((ushort)file.MatVisibilityAnims.Count)
            .U16((ushort)file.ShapeAnims.Count).U16(0).U16((ushort)file.ExternalFiles.Count);
    }
}
