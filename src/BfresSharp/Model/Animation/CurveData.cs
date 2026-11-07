using BfresSharp.Binary;
using BfresSharp.Errors;

namespace BfresSharp.Model.Animation;

/// <summary>Reads and writes the frames and keys of a curve, which are laid out the same on every platform.</summary>
/// <remarks>
/// Frames are 32-bit floats, 16-bit fixed point with five fractional bits, or bytes. Keys are floats, 16-bit or
/// 8-bit integers, a word each for integer steps, bits packed 32 to a word for boolean steps, or the bytes of a
/// compressed curve. Only the byte order differs between platforms, and the view and builder carry it.
/// </remarks>
public static class CurveData
{
    private const ushort PlainKeysFlag = 0x80;

    public static float[] ReadFrames(ByteView view, AnimCurveFrameType type, long at, int count)
    {
        var frames = new float[count];
        for (int index = 0; index < count; index++)
        {
            frames[index] = type switch
            {
                AnimCurveFrameType.Single => view.F32(at + index * 4L),
                AnimCurveFrameType.Decimal10x5 => (short)view.U16(at + index * 2L) / 32f,
                AnimCurveFrameType.Byte => view.U8(at + index),
                _ => throw new BfresFormatException("Invalid frame type", at),
            };
        }

        return frames;
    }

    public static void ReadKeys(ByteView view, AnimCurve curve, long at, int count)
    {
        if (UsesCompressedData(curve))
        {
            curve.CompressedKeyData = view.Slice(at, CompressedLength(curve, count)).Span.ToArray();
            return;
        }

        bool single = curve.KeyType == AnimCurveKeyType.Single;
        if (curve.CurveType == AnimCurveType.StepBool && single)
        {
            curve.BoolKeys = new bool[count];
            for (int key = 0; key < count; key++) curve.BoolKeys[key] = ((view.U32(at + (key / 32) * 4L) >> (key % 32)) & 1) != 0;
            return;
        }

        if (curve.CurveType == AnimCurveType.StepInt && single)
        {
            curve.IntKeys = new int[count];
            for (int key = 0; key < count; key++) curve.IntKeys[key] = view.I32(at + key * 4L);
            return;
        }

        int perKey = curve.ElementsPerKey;
        curve.Keys = new float[count * perKey];
        int width = KeyWidth(curve.KeyType);
        for (int index = 0; index < curve.Keys.Length; index++) curve.Keys[index] = ReadKey(view, curve.KeyType, at + (long)index * width);
    }

    private static float ReadKey(ByteView view, AnimCurveKeyType type, long at)
    {
        return type switch
        {
            AnimCurveKeyType.Single => view.F32(at),
            AnimCurveKeyType.Int16 => (short)view.U16(at),
            AnimCurveKeyType.SByte => (sbyte)view.U8(at),
            _ => throw new BfresFormatException("Invalid key type", at),
        };
    }

    private static void WriteBoolWords(ByteBuilder output, AnimCurve curve)
    {
        // Bits are packed 32 to a word, but the block holds a word for each key; the words past the bits are zero.
        var words = new uint[Math.Max(1, curve.BoolKeys.Length)];
        for (int key = 0; key < curve.BoolKeys.Length; key++)
        {
            if (curve.BoolKeys[key]) words[key / 32] |= 1u << (key % 32);
        }

        foreach (uint word in words) output.U32(word);
    }

    private static void WriteNumbers(ByteBuilder output, AnimCurve curve)
    {
        foreach (float key in curve.Keys)
        {
            switch (curve.KeyType)
            {
                case AnimCurveKeyType.Single: output.F32(key); break;
                case AnimCurveKeyType.Int16: output.U16((ushort)(short)key); break;
                case AnimCurveKeyType.SByte: output.U8((byte)(sbyte)key); break;
            }
        }
    }

    // Bit 7 marks a curve flagged compressed whose keys are still the four plain coefficients a segment.
    public static bool UsesCompressedData(AnimCurve curve)
    {
        if (!curve.IsCompressed || (curve.Flags & PlainKeysFlag) != 0) return false;
        return curve.CurveType is AnimCurveType.Cubic or AnimCurveType.Linear
            or AnimCurveType.StepFloat or AnimCurveType.StepInt or AnimCurveType.StepBool;
    }

    private static int KeyWidth(AnimCurveKeyType type) => type switch
    {
        AnimCurveKeyType.Int16 => 2,
        AnimCurveKeyType.SByte => 1,
        _ => 4,
    };

    // The byte length of a compressed curve's keys, which follows from its type and key width.
    private static int CompressedLength(AnimCurve curve, int count)
    {
        int width = KeyWidth(curve.KeyType);
        bool narrow = curve.KeyType != AnimCurveKeyType.Single;
        return curve.CurveType switch
        {
            AnimCurveType.StepBool => (count + 7) / 8,
            AnimCurveType.Cubic => (narrow ? 8 : 0) + count * 2 * width,
            _ => count * width,
        };
    }
    public static void WriteFrames(ByteBuilder output, AnimCurve curve)
    {
        foreach (float frame in curve.Frames)
        {
            switch (curve.FrameType)
            {
                case AnimCurveFrameType.Single: output.F32(frame); break;
                case AnimCurveFrameType.Decimal10x5: output.U16((ushort)(short)Math.Round(frame * 32f)); break;
                case AnimCurveFrameType.Byte: output.U8((byte)frame); break;
                default: throw new BfresFormatException("Invalid frame type");
            }
        }
    }

    // The array that holds the keys, which a writer can use to tell one curve's keys from another's.
    public static object KeyBlock(AnimCurve curve)
    {
        if (UsesCompressedData(curve)) return curve.CompressedKeyData;
        if (curve.CurveType == AnimCurveType.StepBool && curve.KeyType == AnimCurveKeyType.Single) return curve.BoolKeys;
        if (curve.CurveType == AnimCurveType.StepInt && curve.KeyType == AnimCurveKeyType.Single) return curve.IntKeys;
        return curve.Keys;
    }

    public static void WriteKeys(ByteBuilder output, AnimCurve curve)
    {
        if (UsesCompressedData(curve)) output.Bytes(curve.CompressedKeyData);
        else if (curve.CurveType == AnimCurveType.StepBool && curve.KeyType == AnimCurveKeyType.Single) WriteBoolWords(output, curve);
        else if (curve.CurveType == AnimCurveType.StepInt && curve.KeyType == AnimCurveKeyType.Single)
        {
            foreach (int key in curve.IntKeys) output.U32((uint)key);
        }
        else WriteNumbers(output, curve);
    }
}
