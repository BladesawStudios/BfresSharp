using BfresSharp.Errors;
using BfresSharp.Model.Animation;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes animation curves in the Switch layout.</summary>
/// <remarks>
/// A curve header is 48 bytes: the frame and key pointers, the flags, key count, anim data offset, frame range and
/// the scale, offset and delta. Compressed curves put an unused word before the scale. Frames and keys are stored
/// apart from the header, each padded to 8 bytes. Boolean step keys are packed 32 to a word; a compressed curve keeps
/// its keys as bytes.
/// </remarks>
public static class CurveCodec
{
    public const int HeaderSize = 0x30;
    private const ushort PlainKeysFlag = 0x80;

    public static AnimCurve Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        var curve = new AnimCurve
        {
            Flags = view.U16(at + 0x10),
            AnimDataOffset = view.U32(at + 0x14),
            StartFrame = view.F32(at + 0x18),
            EndFrame = view.F32(at + 0x1C),
        };
        int count = view.U16(at + 0x12);
        long frames = reader.Pointer(at);
        long keys = reader.Pointer(at + 8);
        if (curve.IsCompressed)
        {
            curve.CompressedHeaderWord = view.U32(at + 0x20);
            curve.Scale = view.F32(at + 0x24);
            curve.Offset = view.F32(at + 0x28);
            curve.Delta = view.F32(at + 0x2C);
        }
        else
        {
            curve.Scale = view.F32(at + 0x20);
            curve.Offset = view.F32(at + 0x24);
            curve.Delta = view.F32(at + 0x28);
        }

        curve.Frames = frames == 0 ? [] : ReadFrames(reader, curve.FrameType, frames, count);
        if (keys != 0) ReadKeys(reader, curve, keys, count);
        return curve;
    }

    public static void WriteHeader(SwitchWriter writer, AnimCurve curve, out int framesSlot, out int keysSlot)
    {
        framesSlot = writer.Pointer();
        keysSlot = writer.Pointer();
        writer.Out.U16(curve.Flags).U16((ushort)curve.Frames.Length).U32(curve.AnimDataOffset).F32(curve.StartFrame).F32(curve.EndFrame);
        if (curve.IsCompressed)
            writer.Out.U32(curve.CompressedHeaderWord).F32(curve.Scale).F32(curve.Offset).F32(curve.Delta);
        else
            writer.Out.F32(curve.Scale).F32(curve.Offset).F32(curve.Delta).U32(0);
    }

    public static void WriteFrames(SwitchWriter writer, AnimCurve curve)
    {
        foreach (float frame in curve.Frames)
        {
            switch (curve.FrameType)
            {
                case AnimCurveFrameType.Single: writer.Out.F32(frame); break;
                case AnimCurveFrameType.Decimal10x5: writer.Out.U16((ushort)(short)Math.Round(frame * 32f)); break;
                case AnimCurveFrameType.Byte: writer.Out.U8((byte)frame); break;
                default: throw new BfresFormatException("Invalid frame type");
            }
        }
    }

    public static void WriteKeys(SwitchWriter writer, AnimCurve curve)
    {
        if (UsesCompressedData(curve)) writer.Out.Bytes(curve.CompressedKeyData);
        else if (curve.CurveType == AnimCurveType.StepBool && curve.KeyType == AnimCurveKeyType.Single) WriteBoolWords(writer, curve);
        else if (curve.CurveType == AnimCurveType.StepInt && curve.KeyType == AnimCurveKeyType.Single)
        {
            foreach (int key in curve.IntKeys) writer.Out.U32((uint)key);
        }
        else WriteNumbers(writer, curve);
    }

    private static float[] ReadFrames(SwitchReader reader, AnimCurveFrameType type, long at, int count)
    {
        var view = reader.View;
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

    private static void ReadKeys(SwitchReader reader, AnimCurve curve, long at, int count)
    {
        var view = reader.View;
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
        curve.Keys = new float[count, perKey];
        int width = KeyWidth(curve.KeyType);
        for (int key = 0; key < count; key++)
        {
            for (int element = 0; element < perKey; element++)
                curve.Keys[key, element] = ReadKey(reader, curve.KeyType, at + ((long)key * perKey + element) * width);
        }
    }

    private static float ReadKey(SwitchReader reader, AnimCurveKeyType type, long at)
    {
        return type switch
        {
            AnimCurveKeyType.Single => reader.View.F32(at),
            AnimCurveKeyType.Int16 => (short)reader.View.U16(at),
            AnimCurveKeyType.SByte => (sbyte)reader.View.U8(at),
            _ => throw new BfresFormatException("Invalid key type", at),
        };
    }

    private static void WriteBoolWords(SwitchWriter writer, AnimCurve curve)
    {
        var words = new uint[Math.Max(1, (curve.BoolKeys.Length + 31) / 32)];
        for (int key = 0; key < curve.BoolKeys.Length; key++)
        {
            if (curve.BoolKeys[key]) words[key / 32] |= 1u << (key % 32);
        }

        foreach (uint word in words) writer.Out.U32(word);
    }

    private static void WriteNumbers(SwitchWriter writer, AnimCurve curve)
    {
        foreach (float key in curve.Keys)
        {
            switch (curve.KeyType)
            {
                case AnimCurveKeyType.Single: writer.Out.F32(key); break;
                case AnimCurveKeyType.Int16: writer.Out.U16((ushort)(short)key); break;
                case AnimCurveKeyType.SByte: writer.Out.U8((byte)(sbyte)key); break;
            }
        }
    }

    // Bit 7 marks a curve flagged compressed whose keys are still the four plain coefficients a segment.
    private static bool UsesCompressedData(AnimCurve curve)
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
}
