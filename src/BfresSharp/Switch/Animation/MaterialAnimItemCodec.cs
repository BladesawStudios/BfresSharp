using BfresSharp.Model.Animation;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes the per-material part of an FMAA section in the Switch layout.</summary>
/// <remarks>
/// A 64-byte record: the name and four pointers (parameter entries, pattern entries, curves, constants), five index
/// words, the four counts and six unused bytes. The entries are 24 bytes (parameters) and 16 bytes (patterns); a
/// constant is the field offset and the four value bytes.
/// </remarks>
public static class MaterialAnimItemCodec
{
    public const int Size = 0x40;
    private const int ParamSize = 0x18;
    private const int PatternSize = 0x10;
    private const int ConstantSize = 8;

    public readonly record struct Slots(int Params, int Patterns, int Curves, int Constants);

    public static MaterialAnimItem Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        long counts = at + 0x28;
        int paramCount = view.U16(counts + 10), patternCount = view.U16(counts + 12);
        int constantCount = view.U16(counts + 14), curveCount = view.U16(counts + 16);
        (string name, ulong hash) = reader.NameOrHash(at);
        var item = new MaterialAnimItem
        {
            Name = name,
            NameHash = hash,
            ShaderParamCurveIndex = view.U16(counts),
            TexturePatternCurveIndex = view.U16(counts + 2),
            VisualConstantIndex = view.U16(counts + 4),
            VisualCurveIndex = view.U16(counts + 6),
            BeginVisualConstantIndex = view.U16(counts + 8),
        };
        item.ParamAnimInfos = reader.ReadArray(reader.Pointer(at + 8), paramCount, ParamSize, offset => ReadParam(reader, offset)).ToList();
        item.PatternAnimInfos = reader.ReadArray(reader.Pointer(at + 0x10), patternCount, PatternSize, offset => ReadPattern(reader, offset)).ToList();
        item.Curves = CurveCodec.ReadList(reader, reader.Pointer(at + 0x18), curveCount);
        item.Constants = reader.ReadArray(reader.Pointer(at + 0x20), constantCount, ConstantSize, offset => new AnimConstant(view.U32(offset), view.U32(offset + 4))).ToList();
        return item;
    }

    // Writes the record and returns the slots of its pointers; the entries follow once every record is written.
    public static Slots WriteHeader(SwitchWriter writer, MaterialAnimItem item)
    {
        writer.NamePointer(item.Name, item.NameHash);
        // A pointer to nothing is not recorded.
        int Slot(int count) => count > 0 ? writer.Pointer() : writer.Unlisted();
        var slots = new Slots(Slot(item.ParamAnimInfos.Count), Slot(item.PatternAnimInfos.Count), Slot(item.Curves.Count), Slot(item.Constants.Count));
        writer.Out.U16(item.ShaderParamCurveIndex).U16(item.TexturePatternCurveIndex).U16(item.VisualConstantIndex)
            .U16(item.VisualCurveIndex).U16(item.BeginVisualConstantIndex).U16((ushort)item.ParamAnimInfos.Count)
            .U16((ushort)item.PatternAnimInfos.Count).U16((ushort)item.Constants.Count).U16((ushort)item.Curves.Count).Zeros(6);
        return slots;
    }

    public static void WriteBody(SwitchWriter writer, MaterialAnimItem item, Slots slots)
    {
        if (item.ParamAnimInfos.Count > 0)
        {
            writer.Align(8).Resolve(slots.Params);
            foreach (ParamAnimInfo info in item.ParamAnimInfos) WriteParam(writer, info);
        }

        if (item.PatternAnimInfos.Count > 0)
        {
            writer.Align(8).Resolve(slots.Patterns);
            foreach (PatternAnimInfo info in item.PatternAnimInfos) WritePattern(writer, info);
        }

        if (item.Constants.Count > 0)
        {
            writer.Align(8).Resolve(slots.Constants);
            foreach (AnimConstant constant in item.Constants) writer.Out.U32(constant.AnimDataOffset).U32(constant.Value);
        }

        CurveCodec.WriteList(writer, item.Curves, slots.Curves);
    }

    private static ParamAnimInfo ReadParam(SwitchReader reader, long at)
    {
        var view = reader.View;
        (string name, ulong hash) = reader.NameOrHash(at);
        return new ParamAnimInfo
        {
            Name = name,
            NameHash = hash,
            BeginCurve = view.U16(at + 8),
            FloatCurveCount = view.U16(at + 10),
            IntCurveCount = view.U16(at + 12),
            BeginConstant = view.U16(at + 14),
            ConstantCount = view.U16(at + 16),
            SubBindIndex = view.U16(at + 18),
        };
    }

    private static PatternAnimInfo ReadPattern(SwitchReader reader, long at)
    {
        var view = reader.View;
        (string name, ulong hash) = reader.NameOrHash(at);
        return new PatternAnimInfo
        {
            Name = name,
            NameHash = hash,
            CurveIndex = (short)view.U16(at + 8),
            BeginConstant = view.U16(at + 10),
            SubBindIndex = (sbyte)view.U8(at + 12),
        };
    }

    private static void WriteParam(SwitchWriter writer, ParamAnimInfo info)
    {
        writer.NamePointer(info.Name, info.NameHash);
        writer.Out.U16(info.BeginCurve).U16(info.FloatCurveCount).U16(info.IntCurveCount).U16(info.BeginConstant)
            .U16(info.ConstantCount).U16(info.SubBindIndex).U32(0);
    }

    private static void WritePattern(SwitchWriter writer, PatternAnimInfo info)
    {
        writer.NamePointer(info.Name, info.NameHash);
        writer.Out.U16((ushort)info.CurveIndex).U16(info.BeginConstant).U8((byte)info.SubBindIndex).Zeros(3);
    }
}
