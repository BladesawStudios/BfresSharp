using BfresSharp.Errors;

namespace BfresSharp.Text;

/// <summary>The short forms of a parameter: fixed values as plain numbers, and curves that share their frames as one table.</summary>
/// <remarks>
/// <see cref="Compact"/> rewrites a parameter document when that loses nothing, and <see cref="Expand"/> turns a short
/// form back into the long one the mapper reads. A parameter that does not fit stays in the long form.
/// </remarks>
internal static class ParamShortForm
{
    public static void Compact(ParamDocument parameter)
    {
        if (parameter.BeginCurve is not null || parameter.BeginConstant is not null) return;
        if (parameter.Curves is null && parameter.IntCurves is null && parameter.Constants is { Count: > 0 } constants
            && constants.Select((constant, index) => constant.Raw is null && constant.Texture is null && constant.Value is not null && (constant.Offset ?? 0) == index * 4u).All(fits => fits))
        {
            if (constants.Count == 1) parameter.Value = constants[0].Value;
            else parameter.Values = [.. constants.Select(constant => constant.Value!.Value)];
            parameter.Constants = null;
            return;
        }

        if (parameter.Constants is null && parameter.IntCurves is null && parameter.Curves is { Count: > 1 } curves && Group(curves) is { } group)
        {
            parameter.Animation = group;
            parameter.Curves = null;
        }
    }

    public static ParamDocument Expand(ParamDocument source)
    {
        if (source.Value is null && source.Values is null && source.Color is null && source.Animation is null) return source;
        float[] values = source.Values ?? (source.Value is { } single ? [single] : source.Color is { } color ? ParseColor(color) : []);
        var parameter = new ParamDocument
        {
            Name = source.Name,
            SubBind = source.SubBind,
            Constants = source.Constants,
            Curves = source.Curves,
            IntCurves = source.IntCurves,
            BeginCurve = source.BeginCurve,
            BeginConstant = source.BeginConstant,
        };
        if (values.Length > 0)
        {
            parameter.Constants = [.. parameter.Constants ?? [], .. values.Select((value, index) => new ConstantDocument { Offset = index == 0 ? null : (uint)(index * 4), Value = value })];
        }

        if (source.Animation is { } group) parameter.Curves = [.. parameter.Curves ?? [], .. Split(group)];
        return parameter;
    }

    private static CurveGroupDocument? Group(List<CurveDocument> curves)
    {
        CurveDocument first = curves[0];
        if (first.Keys is null || !Plain(first.Keys[0])) return null;
        bool slopes = first.Keys[0].Slope is not null;
        foreach (CurveDocument curve in curves)
        {
            if (curve.Keys is null || curve.Keys.Count != first.Keys.Count || !SameMeta(first, curve)) return null;
            for (int index = 0; index < curve.Keys.Count; index++)
            {
                KeyDocument key = curve.Keys[index];
                if (key.Frame != first.Keys[index].Frame || !Plain(key) || (key.Slope is not null) != slopes) return null;
            }
        }

        uint[] offsets = [.. curves.Select(curve => curve.Offset ?? 0)];
        if (offsets.Distinct().Count() != offsets.Length) return null;
        return new CurveGroupDocument
        {
            Offsets = offsets,
            Type = first.Type,
            KeyType = first.KeyType,
            FrameType = first.FrameType,
            PreWrap = first.PreWrap,
            PostWrap = first.PostWrap,
            Start = first.Start,
            End = first.End,
            Scale = first.Scale,
            Bias = first.Bias,
            Delta = first.Delta,
            Compressed = first.Compressed,
            OtherFlags = first.OtherFlags,
            HeaderWord = first.HeaderWord,
            Keys = [.. first.Keys.Select((key, index) => new GroupKeyDocument
            {
                Frame = key.Frame,
                Value = [.. curves.Select(curve => curve.Keys![index].Value!.Value)],
                Slope = slopes ? [.. curves.Select(curve => curve.Keys![index].Slope!.Value)] : null,
            })],
        };
    }

    // A key of only a value, or a value and a slope.
    private static bool Plain(KeyDocument key)
        => key.Value is not null && key.Delta is null && key.C1 is null && key.C2 is null && key.C3 is null && key.Int is null && key.Bool is null && key.Texture is null;

    private static bool SameMeta(CurveDocument left, CurveDocument right)
        => left.Type == right.Type && left.KeyType == right.KeyType && left.FrameType == right.FrameType && left.PreWrap == right.PreWrap
        && left.PostWrap == right.PostWrap && left.Start == right.Start && left.End == right.End && left.Scale == right.Scale
        && left.Bias == right.Bias && left.Delta == right.Delta && left.Compressed == right.Compressed
        && left.OtherFlags == right.OtherFlags && left.HeaderWord == right.HeaderWord;

    private static IEnumerable<CurveDocument> Split(CurveGroupDocument group)
    {
        for (int column = 0; column < group.Offsets.Length; column++)
        {
            int at = column;
            yield return new CurveDocument
            {
                Offset = group.Offsets[column] == 0 ? null : group.Offsets[column],
                Type = group.Type,
                KeyType = group.KeyType,
                FrameType = group.FrameType,
                PreWrap = group.PreWrap,
                PostWrap = group.PostWrap,
                Start = group.Start,
                End = group.End,
                Scale = group.Scale,
                Bias = group.Bias,
                Delta = group.Delta,
                Compressed = group.Compressed,
                OtherFlags = group.OtherFlags,
                HeaderWord = group.HeaderWord,
                Keys = [.. group.Keys.Select((key, index) => new KeyDocument
                {
                    Frame = key.Frame,
                    Value = Column(key.Value, at, index, "values", group.Offsets.Length),
                    Slope = key.Slope is null ? null : Column(key.Slope, at, index, "slopes", group.Offsets.Length),
                })],
            };
        }
    }

    private static float Column(float[] values, int column, int key, string what, int expected)
        => column < values.Length ? values[column] : throw new BfresFormatException($"Key {key} has {values.Length} {what} but there are {expected} offsets");

    // "#RRGGBB" or "#RRGGBBAA" as four floats from 0 to 1; without the last pair the alpha is 1, so a colour keeps its four components.
    private static float[] ParseColor(string text)
    {
        string digits = text.TrimStart('#');
        if (digits.Length is not (6 or 8) || !digits.All(Uri.IsHexDigit)) throw new BfresFormatException($"Color \"{text}\" is not #RRGGBB or #RRGGBBAA");
        float[] channels = [.. Enumerable.Range(0, digits.Length / 2).Select(index => Convert.ToInt32(digits.Substring(index * 2, 2), 16) / 255f)];
        return channels.Length == 4 ? channels : [.. channels, 1f];
    }
}
