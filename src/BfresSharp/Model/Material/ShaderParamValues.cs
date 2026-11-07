using System.Buffers.Binary;

namespace BfresSharp.Model.Material;

/// <summary>Reads and writes the values of shader parameters in a material's parameter block.</summary>
/// <remarks>
/// Booleans and integers are stored as 32-bit words and floats as single-precision numbers; structured types such
/// as texture SRTs are plain float runs. Values are little endian for Switch files and big endian for Wii U files,
/// so the caller says which with the big endian argument.
/// </remarks>
public static class ShaderParamValues
{
    public static bool IsFloat(ShaderParamType type) => type is >= ShaderParamType.Float and <= ShaderParamType.TexSrtEx;

    public static float[] ReadFloats(Material material, ShaderParam param, bool bigEndian)
    {
        var floats = new float[param.DataSize / 4];
        for (int index = 0; index < floats.Length; index++)
        {
            ReadOnlySpan<byte> word = material.ShaderParamData.AsSpan(param.DataOffset + index * 4, 4);
            uint raw = bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(word) : BinaryPrimitives.ReadUInt32LittleEndian(word);
            floats[index] = BitConverter.UInt32BitsToSingle(raw);
        }

        return floats;
    }

    public static int[] ReadInts(Material material, ShaderParam param, bool bigEndian)
    {
        var ints = new int[param.DataSize / 4];
        for (int index = 0; index < ints.Length; index++)
        {
            ReadOnlySpan<byte> word = material.ShaderParamData.AsSpan(param.DataOffset + index * 4, 4);
            ints[index] = bigEndian ? BinaryPrimitives.ReadInt32BigEndian(word) : BinaryPrimitives.ReadInt32LittleEndian(word);
        }

        return ints;
    }

    public static void WriteFloats(Material material, ShaderParam param, ReadOnlySpan<float> values, bool bigEndian)
    {
        for (int index = 0; index < values.Length && index < param.DataSize / 4; index++)
        {
            Span<byte> word = material.ShaderParamData.AsSpan(param.DataOffset + index * 4, 4);
            uint raw = BitConverter.SingleToUInt32Bits(values[index]);
            if (bigEndian) BinaryPrimitives.WriteUInt32BigEndian(word, raw);
            else BinaryPrimitives.WriteUInt32LittleEndian(word, raw);
        }
    }

    public static void WriteInts(Material material, ShaderParam param, ReadOnlySpan<int> values, bool bigEndian)
    {
        for (int index = 0; index < values.Length && index < param.DataSize / 4; index++)
        {
            Span<byte> word = material.ShaderParamData.AsSpan(param.DataOffset + index * 4, 4);
            if (bigEndian) BinaryPrimitives.WriteInt32BigEndian(word, values[index]);
            else BinaryPrimitives.WriteInt32LittleEndian(word, values[index]);
        }
    }
}
