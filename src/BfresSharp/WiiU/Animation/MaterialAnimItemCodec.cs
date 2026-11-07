using BfresSharp.Model.Animation;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Animation;

/// <summary>Reads and writes the animation of one material in the Wii U layout, for parameters (32 bytes) and for texture patterns (28 bytes).</summary>
/// <remarks>
/// A parameter item holds the numbers of parameter entries, curves and constants, the index of its first curve and of its
/// first entry, the material name, and offsets to the entries, the curves and the constants. A pattern item holds the
/// numbers of pattern entries and curves, the same two indices, the name, and offsets to the entries, the curves and the
/// base value of each entry. Entries come first, then constants or base values, then the curves and their data.
/// </remarks>
public static class MaterialAnimItemCodec
{
    public const int ParamItemSize = 0x20;
    public const int PatternItemSize = 0x1C;
    private const int ParamInfoSize = 0x10;
    private const int PatternInfoSize = 8;
    private const int ConstantSize = 8;

    public static MaterialAnimItem ReadParam(WiiUReader reader, long at)
    {
        var view = reader.View;
        var item = new MaterialAnimItem
        {
            Name = reader.String(at + 0x10) ?? "",
            ShaderParamCurveIndex = (ushort)view.I32(at + 8),
            InfoIndex = view.I32(at + 0xC),
            ParamAnimInfos = reader.ReadList(at + 0x14, view.U16(at), ParamInfoSize, offset => ReadParamInfo(reader, offset)),
            Curves = CurveCodec.ReadList(reader, at + 0x18, view.U16(at + 2)),
            Constants = reader.ReadList(at + 0x1C, view.U16(at + 4), ConstantSize, offset => new AnimConstant(view.U32(offset), view.U32(offset + 4))),
        };
        return item;
    }

    public static MaterialAnimItem ReadPattern(WiiUReader reader, long at)
    {
        var view = reader.View;
        int count = view.U16(at);
        var item = new MaterialAnimItem
        {
            Name = reader.String(at + 0xC) ?? "",
            TexturePatternCurveIndex = (ushort)view.I32(at + 4),
            InfoIndex = view.I32(at + 8),
            PatternAnimInfos = reader.ReadList(at + 0x10, count, PatternInfoSize, offset => ReadPatternInfo(reader, offset)),
            Curves = CurveCodec.ReadList(reader, at + 0x14, view.U16(at + 2)),
        };
        long baseData = reader.Offset(at + 0x18);
        if (baseData != 0) item.BaseDataList = Enumerable.Range(0, count).Select(index => view.U16(baseData + index * 2L)).ToArray();
        return item;
    }

    public static void WriteParam(WiiUWriter writer, MaterialAnimItem item)
    {
        var o = writer.Out;
        o.U16((ushort)item.ParamAnimInfos.Count).U16((ushort)item.Curves.Count).U16((ushort)item.Constants.Count).U16(0)
            .U32((uint)(int)(short)item.ShaderParamCurveIndex).U32((uint)item.InfoIndex);
        writer.Name(item.Name);
        writer.List(item.ParamAnimInfos, (info, _) => WriteParamInfo(writer, info), pointWhenEmpty: true);
        CurveCodec.WriteList(writer, item.Curves);
        writer.Records(item.Constants, (constant, _) => o.U32(constant.AnimDataOffset).U32(constant.Value), pointWhenEmpty: true);
    }

    // Places what a parameter item refers to: the entries, the constants, the curves, and the data of each curve.
    public static void PlaceParamChildren(WiiUWriter writer, MaterialAnimItem item)
    {
        writer.Place(item.ParamAnimInfos, item.Constants, item.Curves);
        CurveCodec.PlaceData(writer, item.Curves);
    }

    public static void WritePattern(WiiUWriter writer, MaterialAnimItem item)
    {
        var o = writer.Out;
        o.U16((ushort)item.PatternAnimInfos.Count).U16((ushort)item.Curves.Count)
            .U32((uint)(int)(short)item.TexturePatternCurveIndex).U32((uint)item.InfoIndex);
        writer.Name(item.Name);
        writer.List(item.PatternAnimInfos, (info, _) => WritePatternInfo(writer, info), pointWhenEmpty: true);
        CurveCodec.WriteList(writer, item.Curves);
        writer.Custom(item.BaseDataList.Length == 0 ? null : item.BaseDataList, () =>
        {
            foreach (ushort value in item.BaseDataList) o.U16(value);
        });
    }

    // Places what a pattern item refers to: the entries, the curves, the base values, and the data of each curve.
    public static void PlacePatternChildren(WiiUWriter writer, MaterialAnimItem item)
    {
        writer.Place(item.PatternAnimInfos, item.Curves, item.BaseDataList);
        CurveCodec.PlaceData(writer, item.Curves);
    }

    private static ParamAnimInfo ReadParamInfo(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new ParamAnimInfo
        {
            BeginCurve = view.U16(at),
            FloatCurveCount = view.U16(at + 2),
            IntCurveCount = view.U16(at + 4),
            BeginConstant = view.U16(at + 6),
            ConstantCount = view.U16(at + 8),
            SubBindIndex = view.U16(at + 0xA),
            Name = reader.String(at + 0xC) ?? "",
        };
    }

    private static void WriteParamInfo(WiiUWriter writer, ParamAnimInfo info)
    {
        writer.Out.U16(info.BeginCurve).U16(info.FloatCurveCount).U16(info.IntCurveCount).U16(info.BeginConstant).U16(info.ConstantCount).U16(info.SubBindIndex);
        writer.Name(info.Name);
    }

    private static PatternAnimInfo ReadPatternInfo(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new PatternAnimInfo { CurveIndex = (sbyte)view.U8(at), SubBindIndex = (sbyte)view.U8(at + 1), Name = reader.String(at + 4) ?? "" };
    }

    private static void WritePatternInfo(WiiUWriter writer, PatternAnimInfo info)
    {
        writer.Out.U8((byte)(sbyte)info.CurveIndex).U8((byte)info.SubBindIndex).Zeros(2);
        writer.Name(info.Name);
    }
}
