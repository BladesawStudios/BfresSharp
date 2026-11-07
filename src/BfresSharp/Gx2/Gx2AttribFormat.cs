#pragma warning disable 1591

namespace BfresSharp.Gx2;

/// <summary>
/// Represents the format of a vertex attribute entry. Possible type conversions:
/// <para/>UNorm: attrib unsigned integer is converted to/from [0.0, 1.0] in shader.
/// <para/>UInt: attrib unsigned integer is copied to/from shader as unsigned int.
/// <para/>SNorm: attrib signed integer is converted to/from [-1.0, 1.0] in shader.
/// <para/>SInt: attrib signed integer is copied to/from shader as signed int.
/// <para/>Single: attrib single is copied to/from shader as Single.
/// <para/>UIntToSingle: attrib unsigned integer is converted Single in shader.
/// <para/>SIntToSingle: attrib signed integer is converted Single in shader.
/// </summary>
public enum GX2AttribFormat : uint
{
    // 8 bits (8 x 1)
    Format_8_UNorm = 0x00000000,
    Format_8_UInt = 0x00000100,
    Format_8_SNorm = 0x00000200,
    Format_8_SInt = 0x00000300,
    Format_8_UIntToSingle = 0x00000800,
    Format_8_SIntToSingle = 0x00000A00,
    // 8 bits (4 x 2)
    Format_4_4_UNorm = 0x00000001,
    // 16 bits (16 x 1)
    Format_16_UNorm = 0x00000002,
    Format_16_UInt = 0x00000102,
    Format_16_SNorm = 0x00000202,
    Format_16_SInt = 0x00000302,
    Format_16_Single = 0x00000803,
    Format_16_UIntToSingle = 0x00000802,
    Format_16_SIntToSingle = 0x00000A02,
    // 16 bits (8 x 2)
    Format_8_8_UNorm = 0x00000004,
    Format_8_8_UInt = 0x00000104,
    Format_8_8_SNorm = 0x00000204,
    Format_8_8_SInt = 0x00000304,
    Format_8_8_UIntToSingle = 0x00000804,
    Format_8_8_SIntToSingle = 0x00000A04,
    // 32 bits (32 x 1)
    Format_32_UInt = 0x00000105,
    Format_32_SInt = 0x00000305,
    Format_32_Single = 0x00000806,
    // 32 bits (16 x 2)
    Format_16_16_UNorm = 0x00000007,
    Format_16_16_UInt = 0x00000107,
    Format_16_16_SNorm = 0x00000207,
    Format_16_16_SInt = 0x00000307,
    Format_16_16_Single = 0x00000808,
    Format_16_16_UIntToSingle = 0x00000807,
    Format_16_16_SIntToSingle = 0x00000A07,
    // 32 bits (10/11 x 3)
    Format_10_11_11_Single = 0x00000809,
    // 32 bits (8 x 4)
    Format_8_8_8_8_UNorm = 0x0000000A,
    Format_8_8_8_8_UInt = 0x0000010A,
    Format_8_8_8_8_SNorm = 0x0000020A,
    Format_8_8_8_8_SInt = 0x0000030A,
    Format_8_8_8_8_UIntToSingle = 0x0000080A,
    Format_8_8_8_8_SIntToSingle = 0x00000A0A,
    // 32 bits (10 x 3 + 2)
    Format_10_10_10_2_UNorm = 0x0000000B,
    Format_10_10_10_2_UInt = 0x0000010B,
    Format_10_10_10_2_SNorm = 0x0000020B, // High 2 bits are UNorm
    Format_10_10_10_2_SInt = 0x0000030B,
    // 64 bits (32 x 2)
    Format_32_32_UInt = 0x0000010C,
    Format_32_32_SInt = 0x0000030C,
    Format_32_32_Single = 0x0000080D,
    // 64 bits (16 x 4)
    Format_16_16_16_16_UNorm = 0x0000000E,
    Format_16_16_16_16_UInt = 0x0000010E,
    Format_16_16_16_16_SNorm = 0x0000020E,
    Format_16_16_16_16_SInt = 0x0000030E,
    Format_16_16_16_16_Single = 0x0000080F,
    Format_16_16_16_16_UIntToSingle = 0x0000080E,
    Format_16_16_16_16_SIntToSingle = 0x00000A0E,
    // 96 bits (32 x 3)
    Format_32_32_32_UInt = 0x00000110,
    Format_32_32_32_SInt = 0x00000310,
    Format_32_32_32_Single = 0x00000811,
    // 128 bits (32 x 4)
    Format_32_32_32_32_UInt = 0x00000112,
    Format_32_32_32_32_SInt = 0x00000312,
    Format_32_32_32_32_Single = 0x00000813
}
