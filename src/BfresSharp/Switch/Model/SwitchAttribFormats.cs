using BfresSharp.Gx2;

namespace BfresSharp.Switch.Model;

/// <summary>Maps the vertex attribute formats of the GX2 enumeration to the values Switch files store.</summary>
/// <remarks>
/// The model keeps the GX2 names for formats on every platform. Switch files hold a 16-bit code instead, written
/// big-endian, and this table is the one place the two meet, so reading and writing never parse names at run time.
/// </remarks>
public static class SwitchAttribFormats
{
    private static readonly Dictionary<GX2AttribFormat, ushort> ToSwitch = new()
    {
        [GX2AttribFormat.Format_8_UNorm] = 0x0102,
        [GX2AttribFormat.Format_8_UInt] = 0x0302,
        [GX2AttribFormat.Format_8_SNorm] = 0x0202,
        [GX2AttribFormat.Format_8_SInt] = 0x0402,
        [GX2AttribFormat.Format_8_UIntToSingle] = 0x0802,
        [GX2AttribFormat.Format_8_SIntToSingle] = 0x0a02,
        [GX2AttribFormat.Format_4_4_UNorm] = 0x0001,
        [GX2AttribFormat.Format_16_UNorm] = 0x010a,
        [GX2AttribFormat.Format_16_UInt] = 0x030a,
        [GX2AttribFormat.Format_16_SNorm] = 0x020a,
        [GX2AttribFormat.Format_16_SInt] = 0x040a,
        [GX2AttribFormat.Format_16_Single] = 0x050a,
        [GX2AttribFormat.Format_16_UIntToSingle] = 0x0803,
        [GX2AttribFormat.Format_16_SIntToSingle] = 0x0a03,
        [GX2AttribFormat.Format_8_8_UNorm] = 0x0109,
        [GX2AttribFormat.Format_8_8_UInt] = 0x0309,
        [GX2AttribFormat.Format_8_8_SNorm] = 0x0209,
        [GX2AttribFormat.Format_8_8_SInt] = 0x0409,
        [GX2AttribFormat.Format_8_8_UIntToSingle] = 0x0804,
        [GX2AttribFormat.Format_8_8_SIntToSingle] = 0x0a04,
        [GX2AttribFormat.Format_16_16_UNorm] = 0x0112,
        [GX2AttribFormat.Format_16_16_SNorm] = 0x0212,
        [GX2AttribFormat.Format_16_16_UInt] = 0x0312,
        [GX2AttribFormat.Format_16_16_SInt] = 0x0412,
        [GX2AttribFormat.Format_16_16_Single] = 0x0512,
        [GX2AttribFormat.Format_16_16_UIntToSingle] = 0x0807,
        [GX2AttribFormat.Format_16_16_SIntToSingle] = 0x0a07,
        [GX2AttribFormat.Format_10_11_11_Single] = 0x0809,
        [GX2AttribFormat.Format_8_8_8_8_UNorm] = 0x010b,
        [GX2AttribFormat.Format_8_8_8_8_SNorm] = 0x020b,
        [GX2AttribFormat.Format_8_8_8_8_UInt] = 0x030b,
        [GX2AttribFormat.Format_8_8_8_8_SInt] = 0x040b,
        [GX2AttribFormat.Format_8_8_8_8_UIntToSingle] = 0x080b,
        [GX2AttribFormat.Format_8_8_8_8_SIntToSingle] = 0x0a0b,
        [GX2AttribFormat.Format_10_10_10_2_UNorm] = 0x000b,
        [GX2AttribFormat.Format_10_10_10_2_UInt] = 0x090b,
        [GX2AttribFormat.Format_10_10_10_2_SNorm] = 0x020e,
        [GX2AttribFormat.Format_10_10_10_2_SInt] = 0x099b,
        [GX2AttribFormat.Format_16_16_16_16_UNorm] = 0x0115,
        [GX2AttribFormat.Format_16_16_16_16_SNorm] = 0x0215,
        [GX2AttribFormat.Format_16_16_16_16_UInt] = 0x0315,
        [GX2AttribFormat.Format_16_16_16_16_SInt] = 0x0415,
        [GX2AttribFormat.Format_16_16_16_16_Single] = 0x0515,
        [GX2AttribFormat.Format_16_16_16_16_UIntToSingle] = 0x080e,
        [GX2AttribFormat.Format_16_16_16_16_SIntToSingle] = 0x0a0e,
        [GX2AttribFormat.Format_32_UInt] = 0x0314,
        [GX2AttribFormat.Format_32_SInt] = 0x0416,
        [GX2AttribFormat.Format_32_Single] = 0x0516,
        [GX2AttribFormat.Format_32_32_UInt] = 0x0317,
        [GX2AttribFormat.Format_32_32_SInt] = 0x0417,
        [GX2AttribFormat.Format_32_32_Single] = 0x0517,
        [GX2AttribFormat.Format_32_32_32_UInt] = 0x0318,
        [GX2AttribFormat.Format_32_32_32_SInt] = 0x0418,
        [GX2AttribFormat.Format_32_32_32_Single] = 0x0518,
        [GX2AttribFormat.Format_32_32_32_32_UInt] = 0x0319,
        [GX2AttribFormat.Format_32_32_32_32_SInt] = 0x0419,
        [GX2AttribFormat.Format_32_32_32_32_Single] = 0x0519,
    };

    private static readonly Dictionary<ushort, GX2AttribFormat> FromSwitch = ToSwitch.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static ushort Encode(GX2AttribFormat format) => ToSwitch.TryGetValue(format, out ushort value) ? value : throw new Errors.BfresFormatException($"Format {format} has no Switch code");

    public static GX2AttribFormat Decode(ushort code) => FromSwitch.TryGetValue(code, out GX2AttribFormat format) ? format : throw new Errors.BfresFormatException($"Unknown Switch attribute format 0x{code:X}");
}
