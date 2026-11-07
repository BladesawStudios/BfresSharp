using BfresSharp.Model.Animation;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Animation;

/// <summary>Reads and writes the vertex shape animations of an FSHA section, and the key shape infos they hold.</summary>
/// <remarks>
/// An entry is 48 bytes: the name, pointers to the curves, the base data and the infos, the curve and info counts, the
/// two begin indices and padding. An info is 16 bytes: the name, the curve index, the sub bind index and padding.
/// </remarks>
public static class VertexShapeAnimCodec
{
    public const int Size = 0x30;
    private const int InfoSize = 16;

    public static VertexShapeAnim Read(SwitchReader reader, long at)
    {
        var view = reader.View;
        int curves = view.U16(at + 0x20), infos = view.U16(at + 0x22);
        var shape = new VertexShapeAnim { Name = reader.String(at) ?? "", BeginCurve = view.I32(at + 0x24), BeginKeyShapeAnim = view.I32(at + 0x28) };
        long curveArray = reader.Pointer(at + 8), baseData = reader.Pointer(at + 0x10), infoArray = reader.Pointer(at + 0x18);
        if (infoArray != 0) shape.KeyShapeAnimInfos = reader.ReadArray(infoArray, infos, InfoSize, offset => ReadInfo(reader, offset)).ToList();
        if (curveArray != 0) shape.Curves = CurveCodec.ReadList(reader, curveArray, curves);
        if (baseData != 0) shape.BaseData = reader.ReadArray(baseData, Math.Max(infos - 1, 0), 4, offset => view.F32(offset));
        return shape;
    }

    public static Slots WriteHeader(SwitchWriter writer, VertexShapeAnim shape)
    {
        writer.StringPointer(shape.Name);
        var slots = new Slots(writer.Pointer(), writer.Pointer(), writer.Pointer());
        writer.Out.U16((ushort)shape.Curves.Count).U16((ushort)shape.KeyShapeAnimInfos.Count).U32((uint)shape.BeginCurve)
            .U32((uint)shape.BeginKeyShapeAnim).U32(0);
        return slots;
    }

    public static void WriteData(SwitchWriter writer, VertexShapeAnim shape, Slots slots)
    {
        if (shape.KeyShapeAnimInfos.Count > 0)
        {
            writer.Align(8).Resolve(slots.Infos);
            foreach (KeyShapeAnimInfo info in shape.KeyShapeAnimInfos)
            {
                writer.StringPointer(info.Name);
                writer.Out.U8((byte)info.CurveIndex).U8((byte)info.SubBindIndex).Zeros(6);
            }
        }

        if (shape.BaseData.Length > 0)
        {
            writer.Align(8).Resolve(slots.BaseData);
            foreach (float value in shape.BaseData) writer.Out.F32(value);
        }

        CurveCodec.WriteList(writer, shape.Curves, slots.Curves);
    }

    private static KeyShapeAnimInfo ReadInfo(SwitchReader reader, long at)
    {
        return new KeyShapeAnimInfo { Name = reader.String(at) ?? "", CurveIndex = (sbyte)reader.View.U8(at + 8), SubBindIndex = (sbyte)reader.View.U8(at + 9) };
    }

    public readonly record struct Slots(int Curves, int BaseData, int Infos);
}
