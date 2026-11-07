using BfresSharp.Model.Animation;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes animation curves in the Wii U layout, which is 36 bytes from version 3.4.</summary>
/// <remarks>
/// The flags, the number of keys, the offset into the animation data, the first and last frame, the scale and offset and
/// from version 3.4 the delta, then offsets to the frames and the keys. Frames are written before the keys.
/// </remarks>
public static class CurveCodec
{
    private const uint DeltaVersion = 0x03040000;

    public static int Size(uint version) => version >= DeltaVersion ? 0x24 : 0x20;

    public static AnimCurve Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        bool delta = reader.AtLeast(DeltaVersion);
        var curve = new AnimCurve
        {
            Flags = view.U16(at),
            AnimDataOffset = view.U32(at + 4),
            StartFrame = view.F32(at + 8),
            EndFrame = view.F32(at + 0xC),
            Scale = view.F32(at + 0x10),
            Offset = view.F32(at + 0x14),
            Delta = delta ? view.F32(at + 0x18) : 0,
        };
        int count = view.U16(at + 2), next = delta ? 0x1C : 0x18;
        long frames = reader.Offset(at + next), keys = reader.Offset(at + next + 4);
        curve.Frames = frames == 0 ? [] : CurveData.ReadFrames(view, curve.FrameType, frames, count);
        if (keys != 0) CurveData.ReadKeys(view, curve, keys, count);
        return curve;
    }

    public static List<AnimCurve> ReadList(WiiUReader reader, long offsetAt, int count)
    {
        return reader.ReadList(offsetAt, count, Size(reader.Version), at => Read(reader, at));
    }

    // Registers a curve record; its frames and keys are placed by PlaceData.
    public static void Write(WiiUWriter writer, AnimCurve curve)
    {
        var o = writer.Out;
        o.U16(curve.Flags).U16((ushort)curve.Frames.Length).U32(curve.AnimDataOffset).F32(curve.StartFrame).F32(curve.EndFrame)
            .F32(curve.Scale).F32(curve.Offset);
        if (writer.AtLeast(DeltaVersion)) o.F32(curve.Delta);
        writer.Custom(curve.Frames.Length == 0 ? null : curve.Frames, () => CurveData.WriteFrames(o, curve));
        writer.Custom(Keyed(curve), () => CurveData.WriteKeys(o, curve));
    }

    // Registers a section's curves and points the field being written at them.
    public static void WriteList(WiiUWriter writer, List<AnimCurve> curves, bool pointWhenEmpty = true)
    {
        writer.List(curves, (curve, _) => Write(writer, curve), pointWhenEmpty);
    }

    // Places each curve's frames and then its keys, one curve after the other.
    public static void PlaceData(WiiUWriter writer, List<AnimCurve> curves)
    {
        foreach (AnimCurve curve in curves) writer.Place(curve.Frames, Keyed(curve));
    }

    private static object? Keyed(AnimCurve curve) => curve.Frames.Length == 0 ? null : CurveData.KeyBlock(curve);
}
