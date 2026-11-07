using BfresSharp.Gx2;
using BfresSharp.WiiU.Common;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Texture;

/// <summary>Reads and writes textures (FTEX) in the Wii U layout, which is 192 bytes.</summary>
/// <remarks>
/// The GX2 surface (dimension, size, format, tiling, pitch and mip offsets), the view and component selects, the five
/// registers, the array length and the name and path, then offsets to the image data, the mip data and the user data
/// dictionary. The data goes behind the name pool at the alignment of the file.
/// </remarks>
public static class TextureCodec
{
    public const int Size = 0xC0;

    private const int DataRank = 2;

    public static Model.Texture.Texture Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        var texture = new Model.Texture.Texture
        {
            Dim = (GX2SurfaceDim)view.U32(at + 4),
            Width = view.U32(at + 8),
            Height = view.U32(at + 0xC),
            Depth = view.U32(at + 0x10),
            MipCount = view.U32(at + 0x14),
            Format = (GX2SurfaceFormat)view.U32(at + 0x18),
            AAMode = (GX2AAMode)view.U32(at + 0x1C),
            Use = (GX2SurfaceUse)view.U32(at + 0x20),
            DataSize = view.U32(at + 0x24),
            MipDataSize = view.U32(at + 0x2C),
            TileMode = (GX2TileMode)view.U32(at + 0x34),
            Swizzle = view.U32(at + 0x38),
            Alignment = view.U32(at + 0x3C),
            Pitch = view.U32(at + 0x40),
            ViewMipFirst = view.U32(at + 0x78),
            ViewMipCount = view.U32(at + 0x7C),
            ViewSliceFirst = view.U32(at + 0x80),
            ViewSliceCount = view.U32(at + 0x84),
            CompSelR = (GX2CompSel)view.U8(at + 0x88),
            CompSelG = (GX2CompSel)view.U8(at + 0x89),
            CompSelB = (GX2CompSel)view.U8(at + 0x8A),
            CompSelA = (GX2CompSel)view.U8(at + 0x8B),
            ArrayLength = view.U8(at + 0xA4),
            Name = reader.String(at + 0xA8) ?? "",
            Path = reader.String(at + 0xAC) ?? "",
            UserData = UserDataCodec.ReadDict(reader, at + 0xB8),
        };
        for (int index = 0; index < texture.MipOffsets.Length; index++) texture.MipOffsets[index] = view.U32(at + 0x44 + index * 4L);
        for (int index = 0; index < texture.Registers.Length; index++) texture.Registers[index] = view.U32(at + 0x8C + index * 4L);
        long data = reader.LooseOffset(at + 0xB0), mips = reader.LooseOffset(at + 0xB4);
        if (data != 0) (texture.Data, texture.DataPosition) = (reader.Bytes(data, texture.DataSize), (int)data);
        if (mips != 0) (texture.MipData, texture.MipDataPosition) = (reader.Bytes(mips, texture.MipDataSize), (int)mips);
        return texture;
    }

    public static void Write(WiiUWriter writer, Model.Texture.Texture texture)
    {
        var o = writer.Out;
        o.Bytes("FTEX"u8).U32((uint)texture.Dim).U32(texture.Width).U32(texture.Height).U32(texture.Depth).U32(texture.MipCount)
            .U32((uint)texture.Format).U32((uint)texture.AAMode).U32((uint)texture.Use).U32(texture.DataSize).U32(0).U32(texture.MipDataSize).U32(0)
            .U32((uint)texture.TileMode).U32(texture.Swizzle).U32(texture.Alignment).U32(texture.Pitch);
        foreach (uint offset in texture.MipOffsets) o.U32(offset);
        o.U32(texture.ViewMipFirst).U32(texture.ViewMipCount).U32(texture.ViewSliceFirst).U32(texture.ViewSliceCount)
            .U8((byte)texture.CompSelR).U8((byte)texture.CompSelG).U8((byte)texture.CompSelB).U8((byte)texture.CompSelA);
        foreach (uint register in texture.Registers) o.U32(register);
        o.U32(0).U8(texture.ArrayLength).Zeros(3);
        writer.Name(texture.Name);
        writer.Name(texture.Path);
        int alignment = (int)Math.Max(texture.Alignment, 4);
        writer.Block(texture.Data, new Layout(alignment, DataRank, texture.DataPosition));
        writer.Block(texture.MipData, new Layout(alignment, DataRank, texture.MipDataPosition));
        UserDataCodec.WriteDict(writer, texture.UserData);
        writer.PlaceAll(texture.UserData);
        o.U16((ushort)texture.UserData.Count).Zeros(2);
    }
}
