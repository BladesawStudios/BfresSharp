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

        curve.Frames = frames == 0 ? [] : CurveData.ReadFrames(reader.View, curve.FrameType, frames, count);
        if (keys != 0) CurveData.ReadKeys(reader.View, curve, keys, count);
        return curve;
    }

    public static List<AnimCurve> ReadList(SwitchReader reader, long at, int count)
    {
        return reader.ReadArray(at, count, HeaderSize, offset => Read(reader, offset)).ToList();
    }

    // Writes a section's curve headers and points the slot at them; the slots returned feed WriteData.
    public static List<(int Frames, int Keys)> WriteHeaders(SwitchWriter writer, IReadOnlyList<AnimCurve> curves, int slot)
    {
        var slots = new List<(int Frames, int Keys)>();
        if (curves.Count == 0) return slots;
        writer.Align(8).Resolve(slot);
        foreach (AnimCurve curve in curves)
        {
            WriteHeader(writer, curve, out int frames, out int keys);
            slots.Add((frames, keys));
        }

        return slots;
    }

    // Writes each curve's frames and then its keys, each padded to 8 bytes.
    public static void WriteData(SwitchWriter writer, IReadOnlyList<AnimCurve> curves, List<(int Frames, int Keys)> slots)
    {
        for (int index = 0; index < curves.Count; index++)
        {
            writer.Resolve(slots[index].Frames);
            CurveData.WriteFrames(writer.Out, curves[index]);
            writer.Align(8).Resolve(slots[index].Keys);
            CurveData.WriteKeys(writer.Out, curves[index]);
            writer.Align(8);
        }
    }

    public static void WriteList(SwitchWriter writer, IReadOnlyList<AnimCurve> curves, int slot)
    {
        WriteData(writer, curves, WriteHeaders(writer, curves, slot));
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
}
