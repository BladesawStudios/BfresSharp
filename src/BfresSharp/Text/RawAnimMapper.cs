using System.Globalization;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;

namespace BfresSharp.Text;

/// <summary>Converts a <see cref="MaterialAnim"/> to its text document and back.</summary>
/// <remarks>
/// Every field of the model has a place in the document, so a section survives a trip through text unchanged.
/// A document that names an unknown enum value or a bad number is rejected with a <see cref="BfresFormatException"/>.
/// </remarks>
internal static class RawAnimMapper
{
    private const ushort FrameTypeMask = 0b11;
    private const ushort KeyTypeMask = 0b1100;
    private const ushort CurveTypeMask = 0b111_0000;
    private const ushort WrapMask = (3 << 8) | (3 << 12);
    private const ushort NamedMask = FrameTypeMask | KeyTypeMask | CurveTypeMask | WrapMask | AnimCurve.CompressedFlag;

    public static RawAnimDocument ToDocument(MaterialAnim anim) => new()
    {
        Name = anim.Name,
        Path = anim.Path,
        Flags = anim.Flags,
        Pad = anim.Pad,
        FrameCount = anim.FrameCount,
        BakedSize = anim.BakedSize,
        BindIndices = anim.BindIndices,
        TextureNames = anim.TextureNames,
        TextureBinds = anim.TextureBinds,
        Items = [.. anim.Items.Select(ToDocument)],
        UserData = anim.UserData.Count == 0 ? null : [.. anim.UserData.Select(entry => ToUserDocument(entry.Key, entry.Value))],
    };

    public static MaterialAnim FromDocument(RawAnimDocument document, MaterialAnim? original = null)
    {
        var anim = new MaterialAnim
        {
            Name = document.Name,
            Path = document.Path,
            Flags = document.Flags,
            Pad = document.Pad,
            FrameCount = document.FrameCount,
            BakedSize = document.BakedSize,
            BindIndices = document.BindIndices,
            TextureNames = document.TextureNames,
            TextureBinds = document.TextureBinds,
            Items = [.. document.Items.Select((item, index) => FromDocument(item, original?.Items.ElementAtOrDefault(index)))],
        };
        foreach (UserDataDocument entry in document.UserData ?? []) anim.UserData.Add(entry.Name, FromUserDocument(entry));
        return anim;
    }

    private static RawItemDocument ToDocument(MaterialAnimItem item) => new()
    {
        Name = item.Name,
        NameHash = item.NameHash == 0 ? null : item.NameHash,
        ShaderParamCurveIndex = item.ShaderParamCurveIndex,
        TexturePatternCurveIndex = item.TexturePatternCurveIndex,
        VisualConstantIndex = item.VisualConstantIndex,
        VisualCurveIndex = item.VisualCurveIndex,
        BeginVisualConstantIndex = item.BeginVisualConstantIndex,
        InfoIndex = item.InfoIndex,
        BaseDataList = item.BaseDataList.Length == 0 ? null : item.BaseDataList,
        ParamAnimInfos = [.. item.ParamAnimInfos.Select(info => new RawParamDocument
        {
            Name = info.Name,
            NameHash = info.NameHash == 0 ? null : info.NameHash,
            BeginCurve = info.BeginCurve,
            FloatCurveCount = info.FloatCurveCount,
            IntCurveCount = info.IntCurveCount,
            BeginConstant = info.BeginConstant,
            ConstantCount = info.ConstantCount,
            SubBindIndex = info.SubBindIndex,
        })],
        PatternAnimInfos = [.. item.PatternAnimInfos.Select(info => new RawPatternDocument
        {
            Name = info.Name,
            NameHash = info.NameHash == 0 ? null : info.NameHash,
            CurveIndex = info.CurveIndex,
            BeginConstant = info.BeginConstant,
            SubBindIndex = info.SubBindIndex,
        })],
        Curves = [.. item.Curves.Select(ToDocument)],
        Constants = [.. item.Constants.Select(constant => new RawConstantDocument
        {
            Offset = constant.AnimDataOffset,
            Float = constant.AsSingle,
            Value = constant.Value,
        })],
    };

    private static MaterialAnimItem FromDocument(RawItemDocument document, MaterialAnimItem? original) => new()
    {
        Name = document.Name,
        NameHash = document.NameHash ?? 0,
        ShaderParamCurveIndex = document.ShaderParamCurveIndex,
        TexturePatternCurveIndex = document.TexturePatternCurveIndex,
        VisualConstantIndex = document.VisualConstantIndex,
        VisualCurveIndex = document.VisualCurveIndex,
        BeginVisualConstantIndex = document.BeginVisualConstantIndex,
        InfoIndex = document.InfoIndex,
        BaseDataList = document.BaseDataList ?? [],
        ParamAnimInfos = [.. document.ParamAnimInfos.Select(info => new ParamAnimInfo
        {
            Name = info.Name,
            NameHash = info.NameHash ?? 0,
            BeginCurve = info.BeginCurve,
            FloatCurveCount = info.FloatCurveCount,
            IntCurveCount = info.IntCurveCount,
            BeginConstant = info.BeginConstant,
            ConstantCount = info.ConstantCount,
            SubBindIndex = info.SubBindIndex,
        })],
        PatternAnimInfos = [.. document.PatternAnimInfos.Select(info => new PatternAnimInfo
        {
            Name = info.Name,
            NameHash = info.NameHash ?? 0,
            CurveIndex = info.CurveIndex,
            BeginConstant = info.BeginConstant,
            SubBindIndex = info.SubBindIndex,
        })],
        Curves = [.. document.Curves.Select(FromDocument)],
        Constants = [.. document.Constants.Select((constant, index) => ToConstant(constant, original?.Constants.ElementAtOrDefault(index)))],
    };

    // The float and the raw word describe one value. With the constant that was there before, the one that differs
    // from it is the edit; without it the raw word wins, and the float is used only when the word is missing.
    private static AnimConstant ToConstant(RawConstantDocument document, AnimConstant? original)
    {
        uint? floatWord = document.Float is { } number ? BitConverter.SingleToUInt32Bits(number) : null;
        uint word = document.Value ?? floatWord ?? 0;
        if (original is { } before && document.Value == before.Value && floatWord is { } edited) word = edited;
        return new AnimConstant(document.Offset, word);
    }

    private static RawCurveDocument ToDocument(AnimCurve curve)
    {
        bool compressed = curve.IsCompressed && CurveData.UsesCompressedData(curve);
        return new RawCurveDocument
        {
            FrameType = curve.FrameType.ToString(),
            KeyType = curve.KeyType.ToString(),
            CurveType = curve.CurveType.ToString(),
            PreWrap = curve.PreWrap.ToString(),
            PostWrap = curve.PostWrap.ToString(),
            Compressed = curve.IsCompressed,
            ExtraFlags = (ushort)(curve.Flags & ~NamedMask),
            AnimDataOffset = curve.AnimDataOffset,
            StartFrame = curve.StartFrame,
            EndFrame = curve.EndFrame,
            Scale = curve.Scale,
            Offset = curve.Offset,
            Delta = curve.Delta,
            CompressedHeaderWord = curve.CompressedHeaderWord == 0 ? null : curve.CompressedHeaderWord,
            Frames = curve.Frames,
            Keys = !compressed && curve.Keys.Length > 0 ? curve.Keys : null,
            IntKeys = !compressed && curve.IntKeys.Length > 0 ? curve.IntKeys : null,
            BoolKeys = !compressed && curve.BoolKeys.Length > 0 ? curve.BoolKeys : null,
            CompressedKeyData = compressed ? Convert.ToHexString(curve.CompressedKeyData) : null,
        };
    }

    private static AnimCurve FromDocument(RawCurveDocument document)
    {
        var curve = new AnimCurve
        {
            Flags = document.ExtraFlags,
            FrameType = ParseEnum<AnimCurveFrameType>(document.FrameType, "FrameType"),
            KeyType = ParseEnum<AnimCurveKeyType>(document.KeyType, "KeyType"),
            CurveType = ParseEnum<AnimCurveType>(document.CurveType, "CurveType"),
            PreWrap = ParseWrap(document.PreWrap, "PreWrap"),
            PostWrap = ParseWrap(document.PostWrap, "PostWrap"),
            AnimDataOffset = document.AnimDataOffset,
            StartFrame = document.StartFrame,
            EndFrame = document.EndFrame,
            Scale = document.Scale,
            Offset = document.Offset,
            Delta = document.Delta,
            CompressedHeaderWord = document.CompressedHeaderWord ?? 0,
            Frames = document.Frames,
            Keys = document.Keys ?? [],
            IntKeys = document.IntKeys ?? [],
            BoolKeys = document.BoolKeys ?? [],
        };
        // The setters above keep the other bits, so ExtraFlags only seeded the bits nothing named owns.
        curve.Flags = (ushort)(curve.Flags & ~AnimCurve.CompressedFlag | (document.Compressed ? AnimCurve.CompressedFlag : 0));
        if (document.CompressedKeyData is { } hex) curve.CompressedKeyData = ParseHex(hex, "CompressedKeyData");
        return curve;
    }

    internal static UserDataDocument ToUserDocument(string name, UserData entry) => new()
    {
        Name = name,
        Type = entry.Type.ToString(),
        Values = [.. entry.Value.Cast<object>().Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "")],
        Reserved = entry.Reserved.All(b => b == 0) ? null : Convert.ToHexString(entry.Reserved),
    };

    internal static UserData FromUserDocument(UserDataDocument document)
    {
        var entry = new UserData { Name = document.Name };
        IEnumerable<string> values = document.Values;
        switch (ParseEnum<UserDataType>(document.Type, "Type"))
        {
            case UserDataType.Int32: entry.SetInts([.. values.Select(value => Parse(value, int.Parse))]); break;
            case UserDataType.Single: entry.SetFloats([.. values.Select(value => Parse(value, float.Parse))]); break;
            case UserDataType.String: entry.SetStrings([.. values]); break;
            case UserDataType.WString: entry.SetStrings([.. values], wide: true); break;
            case UserDataType.Byte: entry.SetBytes([.. values.Select(value => Parse(value, sbyte.Parse))]); break;
        }
        if (document.Reserved is { } hex) entry.Reserved = ParseHex(hex, "Reserved");
        return entry;
    }

    private static T Parse<T>(string text, Func<string, IFormatProvider, T> parse)
    {
        try
        {
            return parse(text, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new BfresFormatException($"\"{text}\" is not a valid number");
        }
        catch (OverflowException)
        {
            throw new BfresFormatException($"\"{text}\" is out of range");
        }
    }

    // The wrap field has two bits and files use all four values, though only three have names; the fourth reads as its number.
    private static WrapMode ParseWrap(string text, string field)
    {
        if (int.TryParse(text, out int number) && number is >= 0 and <= 3) return (WrapMode)number;
        return ParseEnum<WrapMode>(text, field);
    }

    private static T ParseEnum<T>(string text, string field) where T : struct, Enum
    {
        if (Enum.TryParse(text, ignoreCase: false, out T value) && Enum.IsDefined(value)) return value;
        throw new BfresFormatException($"{field} \"{text}\" is not one of: {string.Join(", ", Enum.GetNames<T>())}");
    }

    private static byte[] ParseHex(string text, string field)
    {
        try
        {
            return Convert.FromHexString(text);
        }
        catch (FormatException)
        {
            throw new BfresFormatException($"{field} is not a hex string");
        }
    }
}
