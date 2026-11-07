using System.Buffers.Binary;
using System.Globalization;
using BfresSharp.Errors;
using BfresSharp.Model.Animation;

namespace BfresSharp.Text;

/// <summary>Converts a <see cref="MaterialAnim"/> to its readable document and back.</summary>
/// <remarks>
/// Parameters and patterns own the curves and constants they use, so the document has no indices. Reading it back lays
/// the curves and constants out in the order the files use (parameters, then patterns, then visibility) and works the
/// indices out from that. The caller checks the result against the original, because a file that lays them out some
/// other way cannot be shown this way.
/// </remarks>
internal static class AnimMapper
{
    private const ushort None = ushort.MaxValue;
    private const ushort NamedMask = 0b11 | 0b1100 | 0b111_0000 | (3 << 8) | (3 << 12) | AnimCurve.CompressedFlag;

    public static AnimDocument ToDocument(MaterialAnim anim, NameTable names)
    {
        List<string> textures = anim.TextureNames;
        var document = new AnimDocument
        {
            Name = anim.Name,
            Path = anim.Path,
            Frames = anim.FrameCount,
            Loop = anim.Loop,
            Baked = anim.Baked,
            OtherFlags = anim.Flags is var flags && (flags & ~5) != 0 ? (ushort)(flags & ~5) : null,
            Pad = anim.Pad == 0 ? null : anim.Pad,
            BakedSize = anim.BakedSize == 0 ? null : anim.BakedSize,
            BindIndices = anim.BindIndices,
            Textures = textures,
            TextureBinds = anim.TextureBinds.Length == textures.Count && anim.TextureBinds.All(bind => bind == -1) ? null : anim.TextureBinds,
            UserData = anim.UserData.Count == 0 ? null : [.. anim.UserData.Select(entry => RawAnimMapper.ToUserDocument(entry.Key, entry.Value))],
        };
        int curveStart = 0, constantStart = 0;
        foreach (MaterialAnimItem item in anim.Items)
        {
            document.Materials.Add(ToDocument(item, textures, curveStart, constantStart, names));
            curveStart += item.Curves.Count;
            constantStart += item.Constants.Count;
        }

        return document;
    }

    public static MaterialAnim FromDocument(AnimDocument document, NameTable names)
    {
        var anim = new MaterialAnim
        {
            Name = document.Name,
            Path = document.Path,
            FrameCount = document.Frames,
            Flags = (ushort)((document.OtherFlags ?? 0) & ~5 | (document.Baked ? 1 : 0) | (document.Loop ? 4 : 0)),
            Pad = document.Pad ?? 0,
            BakedSize = document.BakedSize ?? 0,
            BindIndices = document.BindIndices,
            TextureNames = document.Textures,
            TextureBinds = document.TextureBinds ?? [.. document.Textures.Select(_ => -1L)],
        };
        int curveStart = 0, constantStart = 0;
        foreach (MaterialDocument material in document.Materials)
        {
            MaterialAnimItem item = FromDocument(material, document.Textures, curveStart, constantStart, names);
            anim.Items.Add(item);
            curveStart += item.Curves.Count;
            constantStart += item.Constants.Count;
        }

        foreach (UserDataDocument entry in document.UserData ?? []) anim.UserData.Add(entry.Name, RawAnimMapper.FromUserDocument(entry));
        return anim;
    }

    private static MaterialDocument ToDocument(MaterialAnimItem item, List<string> textures, int curveStart, int constantStart, NameTable names)
    {
        var claimedCurves = new bool[item.Curves.Count];
        var claimedConstants = new bool[item.Constants.Count];
        int runningCurve = 0, runningConstant = 0;
        var document = new MaterialDocument { Material = names.Show(item.Name, item.NameHash) };

        var parameters = new List<ParamDocument>();
        foreach (ParamAnimInfo info in item.ParamAnimInfos)
        {
            int curveCount = info.FloatCurveCount + info.IntCurveCount;
            var parameter = new ParamDocument
            {
                Name = names.Show(info.Name, info.NameHash),
                SubBind = info.SubBindIndex == None ? null : info.SubBindIndex,
                BeginCurve = info.BeginCurve == runningCurve ? null : info.BeginCurve,
                BeginConstant = info.BeginConstant == runningConstant ? null : info.BeginConstant,
            };
            List<CurveDocument> curves = Take(item.Curves, claimedCurves, info.BeginCurve, curveCount).Select(curve => ToDocument(curve, null)).ToList();
            parameter.Curves = curves.Take(info.FloatCurveCount).ToList() is { Count: > 0 } floats ? floats : null;
            parameter.IntCurves = curves.Skip(info.FloatCurveCount).ToList() is { Count: > 0 } ints ? ints : null;
            List<ConstantDocument> constants = Take(item.Constants, claimedConstants, info.BeginConstant, info.ConstantCount).Select(constant => ToDocument(constant, null)).ToList();
            parameter.Constants = constants.Count == 0 ? null : constants;
            ParamShortForm.Compact(parameter);
            runningCurve += curveCount;
            runningConstant += info.ConstantCount;
            parameters.Add(parameter);
        }

        int patternCurveStart = runningCurve;
        var patterns = new List<PatternDocument>();
        foreach (PatternAnimInfo info in item.PatternAnimInfos)
        {
            var pattern = new PatternDocument
            {
                Name = names.Show(info.Name, info.NameHash),
                SubBind = info.SubBindIndex == -1 ? null : info.SubBindIndex,
            };
            if (info.CurveIndex >= 0)
            {
                pattern.Curve = Take(item.Curves, claimedCurves, info.CurveIndex, 1).Select(curve => ToDocument(curve, textures)).FirstOrDefault();
                pattern.CurveIndex = info.CurveIndex == runningCurve ? null : info.CurveIndex;
                pattern.BeginConstant = info.BeginConstant == None ? null : info.BeginConstant;
                runningCurve++;
            }
            else
            {
                if (info.BeginConstant != None)
                {
                    pattern.Constant = Take(item.Constants, claimedConstants, info.BeginConstant, 1).Select(constant => ToDocument(constant, textures)).FirstOrDefault();
                    pattern.BeginConstant = info.BeginConstant == runningConstant ? null : info.BeginConstant;
                    runningConstant++;
                }

                pattern.CurveIndex = info.CurveIndex == -1 ? null : info.CurveIndex;
            }

            if (pattern is { Curve: null, Constant: { Texture: { } texture, Offset: null }, BeginConstant: null, CurveIndex: null })
            {
                pattern.Texture = texture;
                pattern.Constant = null;
            }

            patterns.Add(pattern);
        }

        document.Params = parameters.Count == 0 ? null : parameters;
        document.Patterns = patterns.Count == 0 ? null : patterns;
        List<CurveDocument> visibleCurves = item.Curves.Where((_, index) => !claimedCurves[index]).Select(curve => ToDocument(curve, null)).ToList();
        List<ConstantDocument> visibleConstants = item.Constants.Where((_, index) => !claimedConstants[index]).Select(constant => ToDocument(constant, null)).ToList();
        if (visibleCurves.Count + visibleConstants.Count > 0)
        {
            document.Visibility = new VisibilityDocument
            {
                Curves = visibleCurves.Count == 0 ? null : visibleCurves,
                Constants = visibleConstants.Count == 0 ? null : visibleConstants,
            };
        }

        IndicesDocument actual = new()
        {
            ShaderParamCurve = item.ShaderParamCurveIndex,
            TexturePatternCurve = item.TexturePatternCurveIndex,
            VisualConstant = item.VisualConstantIndex,
            VisualCurve = item.VisualCurveIndex,
            BeginVisualConstant = item.BeginVisualConstantIndex,
        };
        if (!SameIndices(actual, Derive(item.ParamAnimInfos.Count, item.PatternAnimInfos.Count, patternCurveStart, runningCurve, curveStart, constantStart + runningConstant, visibleCurves.Count, visibleConstants.Count))) document.Indices = actual;
        document.InfoIndex = item.InfoIndex == 0 ? null : item.InfoIndex;
        document.BaseData = item.BaseDataList.Length == 0 ? null : item.BaseDataList;
        return document;
    }

    private static MaterialAnimItem FromDocument(MaterialDocument document, List<string> textures, int curveStart, int constantStart, NameTable names)
    {
        var item = new MaterialAnimItem
        {
            Name = names.Store(document.Material),
            NameHash = names.HashOf(document.Material),
            InfoIndex = document.InfoIndex ?? 0,
            BaseDataList = document.BaseData ?? [],
        };
        foreach (ParamDocument shortForm in document.Params ?? [])
        {
            ParamDocument parameter = ParamShortForm.Expand(shortForm);
            List<CurveDocument> floatCurves = parameter.Curves ?? [], intCurves = parameter.IntCurves ?? [];
            List<ConstantDocument> constants = parameter.Constants ?? [];
            item.ParamAnimInfos.Add(new ParamAnimInfo
            {
                Name = names.Store(parameter.Name),
                NameHash = names.HashOf(parameter.Name),
                BeginCurve = parameter.BeginCurve ?? (ushort)item.Curves.Count,
                FloatCurveCount = (ushort)floatCurves.Count,
                IntCurveCount = (ushort)intCurves.Count,
                BeginConstant = parameter.BeginConstant ?? (ushort)item.Constants.Count,
                ConstantCount = (ushort)constants.Count,
                SubBindIndex = parameter.SubBind ?? None,
            });
            item.Curves.AddRange(floatCurves.Concat(intCurves).Select(curve => FromDocument(curve, null)));
            item.Constants.AddRange(constants.Select(constant => FromDocument(constant, null)));
        }

        int patternCurveStart = item.Curves.Count;
        foreach (PatternDocument shortForm in document.Patterns ?? [])
        {
            PatternDocument pattern = shortForm.Texture is null ? shortForm : new PatternDocument { Name = shortForm.Name, SubBind = shortForm.SubBind, Constant = new ConstantDocument { Texture = shortForm.Texture } };
            short curveIndex = pattern.CurveIndex ?? (pattern.Curve is null ? (short)-1 : (short)item.Curves.Count);
            ushort beginConstant = pattern.BeginConstant ?? (pattern.Constant is null || pattern.Curve is not null ? None : (ushort)item.Constants.Count);
            item.PatternAnimInfos.Add(new PatternAnimInfo
            {
                Name = names.Store(pattern.Name),
                NameHash = names.HashOf(pattern.Name),
                CurveIndex = curveIndex,
                BeginConstant = beginConstant,
                SubBindIndex = pattern.SubBind ?? -1,
            });
            if (pattern.Curve is not null) item.Curves.Add(FromDocument(pattern.Curve, textures));
            if (pattern.Constant is not null) item.Constants.Add(FromDocument(pattern.Constant, textures));
        }

        int patternCurveEnd = item.Curves.Count;
        item.Curves.AddRange((document.Visibility?.Curves ?? []).Select(curve => FromDocument(curve, null)));
        item.Constants.AddRange((document.Visibility?.Constants ?? []).Select(constant => FromDocument(constant, null)));
        IndicesDocument indices = document.Indices ?? Derive(item.ParamAnimInfos.Count, item.PatternAnimInfos.Count, patternCurveStart, patternCurveEnd, curveStart, constantStart + item.Constants.Count - (document.Visibility?.Constants?.Count ?? 0), document.Visibility?.Curves?.Count ?? 0, document.Visibility?.Constants?.Count ?? 0);
        item.ShaderParamCurveIndex = indices.ShaderParamCurve;
        item.TexturePatternCurveIndex = indices.TexturePatternCurve;
        item.VisualConstantIndex = indices.VisualConstant;
        item.VisualCurveIndex = indices.VisualCurve;
        item.BeginVisualConstantIndex = indices.BeginVisualConstant;
        return item;
    }

    // What the files show: an index points at the first curve of its group in the whole animation's curve list, and is
    // all ones when the material has no such group. The visibility constant words are a guess that is checked on writing.
    private static IndicesDocument Derive(int parameters, int patterns, int patternStart, int patternEnd, int curveStart, int visualConstantStart, int visualCurves, int visualConstants) => new()
    {
        ShaderParamCurve = parameters > 0 ? Cast(curveStart) : None,
        TexturePatternCurve = patterns > 0 ? Cast(curveStart + patternStart) : None,
        VisualConstant = visualConstants > 0 ? Cast(visualConstantStart) : None,
        VisualCurve = visualCurves > 0 ? Cast(curveStart + patternEnd) : None,
        BeginVisualConstant = visualConstants > 0 ? (ushort)0 : None,
    };

    private static ushort Cast(int value) => (ushort)Math.Clamp(value, 0, None);

    private static bool SameIndices(IndicesDocument left, IndicesDocument right)
        => left.ShaderParamCurve == right.ShaderParamCurve && left.TexturePatternCurve == right.TexturePatternCurve
        && left.VisualConstant == right.VisualConstant && left.VisualCurve == right.VisualCurve && left.BeginVisualConstant == right.BeginVisualConstant;

    // Marks a range as owned by an entry. A range past the end of the list is cut short; the caller's check catches the loss.
    private static IEnumerable<T> Take<T>(List<T> list, bool[] claimed, int begin, int count)
    {
        for (int index = begin; index < begin + count && index < list.Count && index >= 0; index++)
        {
            claimed[index] = true;
            yield return list[index];
        }
    }

    private static ConstantDocument ToDocument(AnimConstant constant, List<string>? textures)
    {
        var document = new ConstantDocument { Offset = constant.AnimDataOffset == 0 ? null : constant.AnimDataOffset };
        if (textures is not null && constant.AsInt32 >= 0 && constant.AsInt32 < textures.Count) document.Texture = textures[constant.AsInt32];
        else if (textures is null && IsPlainFloat(constant.Value)) document.Value = constant.AsSingle;
        else document.Raw = constant.AsInt32;
        return document;
    }

    private static AnimConstant FromDocument(ConstantDocument document, List<string>? textures)
    {
        uint word = document switch
        {
            { Texture: { } name } => (uint)TextureIndex(name, textures),
            { Value: { } value } => BitConverter.SingleToUInt32Bits(value),
            { Raw: { } raw } => unchecked((uint)raw),
            _ => throw new BfresFormatException("A constant needs a value"),
        };
        return new AnimConstant(document.Offset ?? 0, word);
    }

    // A word that reads as a float of ordinary size; anything else (small integers, flags, NaN) is shown as an integer.
    private static bool IsPlainFloat(uint word)
    {
        if (word == 0) return true;
        float value = BitConverter.UInt32BitsToSingle(word);
        float magnitude = Math.Abs(value);
        return float.IsFinite(value) && magnitude >= 1e-6f && magnitude <= 1e9f;
    }

    private static int TextureIndex(string name, List<string>? textures)
    {
        int index = textures?.IndexOf(name) ?? -1;
        if (index >= 0) return index;
        throw new BfresFormatException($"Texture \"{name}\" is not one of: {string.Join(", ", textures ?? [])}");
    }

    private static CurveDocument ToDocument(AnimCurve curve, List<string>? textures)
    {
        var document = new CurveDocument
        {
            Offset = curve.AnimDataOffset == 0 ? null : curve.AnimDataOffset,
            Type = curve.CurveType.ToString(),
            KeyType = curve.KeyType == AnimCurveKeyType.Single ? null : curve.KeyType.ToString(),
            FrameType = curve.FrameType == AnimCurveFrameType.Single ? null : curve.FrameType.ToString(),
            PreWrap = curve.PreWrap == WrapMode.Clamp ? null : WrapText(curve.PreWrap),
            PostWrap = curve.PostWrap == WrapMode.Clamp ? null : WrapText(curve.PostWrap),
            Start = curve.StartFrame,
            End = curve.EndFrame,
            Scale = curve.Scale == 0 ? null : curve.Scale,
            Bias = curve.Offset == 0 ? null : curve.Offset,
            Delta = curve.Delta == 0 ? null : curve.Delta,
            Compressed = curve.IsCompressed ? true : null,
            OtherFlags = (curve.Flags & ~NamedMask) == 0 ? null : (ushort)(curve.Flags & ~NamedMask),
            HeaderWord = curve.CompressedHeaderWord == 0 ? null : curve.CompressedHeaderWord,
        };
        List<KeyDocument>? keys = KeyCodec.Decode(curve, textures);
        if (keys is not null)
        {
            document.Keys = keys;
            return document;
        }

        // Keys that do not line up with the frames, or a packing this does not decode, are written as plain lists.
        document.Frames = curve.Frames;
        if (CurveData.UsesCompressedData(curve)) document.Data = Convert.ToHexString(curve.CompressedKeyData);
        else
        {
            document.Values = curve.Keys.Length == 0 ? null : curve.Keys;
            document.IntValues = curve.IntKeys.Length == 0 ? null : curve.IntKeys;
            document.BoolValues = curve.BoolKeys.Length == 0 ? null : curve.BoolKeys;
        }

        return document;
    }

    private static AnimCurve FromDocument(CurveDocument document, List<string>? textures)
    {
        var curve = new AnimCurve
        {
            Flags = (ushort)((document.OtherFlags ?? 0) & ~NamedMask),
            AnimDataOffset = document.Offset ?? 0,
            StartFrame = document.Start,
            EndFrame = document.End,
            Scale = document.Scale ?? 0,
            Offset = document.Bias ?? 0,
            Delta = document.Delta ?? 0,
            CompressedHeaderWord = document.HeaderWord ?? 0,
        };
        curve.CurveType = ParseEnum<AnimCurveType>(document.Type, "type");
        curve.KeyType = document.KeyType is null ? AnimCurveKeyType.Single : ParseEnum<AnimCurveKeyType>(document.KeyType, "keyType");
        curve.FrameType = document.FrameType is null ? AnimCurveFrameType.Single : ParseEnum<AnimCurveFrameType>(document.FrameType, "frameType");
        curve.PreWrap = ParseWrap(document.PreWrap, "preWrap");
        curve.PostWrap = ParseWrap(document.PostWrap, "postWrap");
        if (document.Compressed == true) curve.Flags |= AnimCurve.CompressedFlag;

        if (document.Keys is not null)
        {
            KeyCodec.Encode(curve, document.Keys, textures);
            return curve;
        }

        curve.Frames = document.Frames ?? [];
        curve.Keys = document.Values ?? [];
        curve.IntKeys = document.IntValues ?? [];
        curve.BoolKeys = document.BoolValues ?? [];
        if (document.Data is { } hex) curve.CompressedKeyData = ParseHex(hex);
        return curve;
    }

    private static string WrapText(WrapMode mode) => Enum.IsDefined(mode) ? mode.ToString() : ((int)mode).ToString(CultureInfo.InvariantCulture);

    private static WrapMode ParseWrap(string? text, string field)
    {
        if (text is null) return WrapMode.Clamp;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number is >= 0 and <= 3) return (WrapMode)number;
        return ParseEnum<WrapMode>(text, field);
    }

    private static T ParseEnum<T>(string text, string field) where T : struct, Enum
    {
        if (Enum.TryParse(text, ignoreCase: true, out T value) && Enum.IsDefined(value)) return value;
        throw new BfresFormatException($"{field} \"{text}\" is not one of: {string.Join(", ", Enum.GetNames<T>())}");
    }

    private static byte[] ParseHex(string text)
    {
        try
        {
            return Convert.FromHexString(text);
        }
        catch (FormatException)
        {
            throw new BfresFormatException("data is not a hex string");
        }
    }

    /// <summary>Turns the key storage of a curve into rows of frame and value, and back.</summary>
    /// <remarks>
    /// A compressed curve with full-width keys is a plain run of little-endian floats; with 8 or 16-bit integer keys
    /// the step kinds are a run of integers. Other packings (narrow float keys carry a scale and bias, narrow cubic
    /// keys a header) are left as bytes.
    /// </remarks>
    private static class KeyCodec
    {
        public static List<KeyDocument>? Decode(AnimCurve curve, List<string>? textures)
        {
            int count = curve.Frames.Length;
            if (count == 0) return null;
            var keys = new List<KeyDocument>(count);
            for (int index = 0; index < count; index++) keys.Add(new KeyDocument { Frame = curve.Frames[index] });
            if (CurveData.UsesCompressedData(curve)) return DecodeCompressed(curve, keys, textures) && EncodesBack(curve, keys, textures) ? keys : null;
            if (curve.CurveType == AnimCurveType.StepBool && curve.KeyType == AnimCurveKeyType.Single)
            {
                if (curve.BoolKeys.Length != count) return null;
                for (int index = 0; index < count; index++) keys[index].Bool = curve.BoolKeys[index];
                return keys;
            }

            if (curve.CurveType == AnimCurveType.StepInt && curve.KeyType == AnimCurveKeyType.Single)
            {
                if (curve.IntKeys.Length != count) return null;
                for (int index = 0; index < count; index++) SetInt(keys[index], curve.IntKeys[index], textures);
                return keys;
            }

            int perKey = curve.ElementsPerKey;
            if (curve.CurveType is AnimCurveType.BakedFloat or AnimCurveType.BakedInt or AnimCurveType.BakedBool or AnimCurveType.StepInt or AnimCurveType.StepBool || curve.Keys.Length != count * perKey) return null;
            for (int index = 0; index < count; index++)
            {
                ReadOnlySpan<float> key = curve.Keys.AsSpan(index * perKey, perKey);
                keys[index].Value = key[0];
                if (curve.CurveType == AnimCurveType.Linear) keys[index].Delta = key[1];
                if (curve.CurveType == AnimCurveType.Cubic)
                {
                    keys[index].C1 = key[1];
                    keys[index].C2 = key[2];
                    keys[index].C3 = key[3];
                }
            }

            return keys;
        }

        // Rows are used only when they write the same bytes, which a narrow curve with a zero scale or odd steps does not.
        private static bool EncodesBack(AnimCurve curve, List<KeyDocument> keys, List<string>? textures)
        {
            var probe = new AnimCurve { Flags = curve.Flags, Scale = curve.Scale, Offset = curve.Offset };
            try
            {
                Encode(probe, keys, textures);
            }
            catch (BfresFormatException)
            {
                return false;
            }

            return probe.CompressedKeyData.AsSpan().SequenceEqual(curve.CompressedKeyData) && probe.Scale == curve.Scale && probe.Offset == curve.Offset;
        }

        private static bool DecodeCompressed(AnimCurve curve, List<KeyDocument> keys, List<string>? textures)
        {
            byte[] data = curve.CompressedKeyData;
            int count = keys.Count;
            int width = curve.KeyType switch { AnimCurveKeyType.Int16 => 2, AnimCurveKeyType.SByte => 1, _ => 4 };
            if (curve.CurveType == AnimCurveType.StepBool)
            {
                if (data.Length != (count + 7) / 8) return false;
                for (int index = 0; index < count; index++) keys[index].Bool = (data[index / 8] >> (index % 8) & 1) != 0;
                return true;
            }

            if (curve.CurveType == AnimCurveType.StepInt)
            {
                if (data.Length != count * width) return false;
                for (int index = 0; index < count; index++) SetInt(keys[index], (int)Math.Round(Dequantize(curve, Quantized(data, index, width))), textures);
                return true;
            }

            if (curve.KeyType != AnimCurveKeyType.Single)
            {
                // Narrow keys are unsigned steps of the curve's scale from its bias.
                if (curve.CurveType is not (AnimCurveType.Linear or AnimCurveType.StepFloat) || data.Length != count * width) return false;
                for (int index = 0; index < count; index++) keys[index].Value = (float)Dequantize(curve, Quantized(data, index, width));
                return true;
            }

            int perKey = curve.CurveType == AnimCurveType.Cubic ? 2 : 1;
            if (curve.CurveType is not (AnimCurveType.Cubic or AnimCurveType.Linear or AnimCurveType.StepFloat) || data.Length != count * perKey * 4) return false;
            for (int index = 0; index < count; index++)
            {
                keys[index].Value = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(index * perKey * 4));
                if (perKey == 2) keys[index].Slope = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(index * 8 + 4));
            }

            return true;
        }

        private static void SetInt(KeyDocument key, int value, List<string>? textures)
        {
            if (textures is not null && value >= 0 && value < textures.Count) key.Texture = textures[value];
            else key.Int = value;
        }

        public static void Encode(AnimCurve curve, List<KeyDocument> keys, List<string>? textures)
        {
            int count = keys.Count;
            curve.Frames = [.. keys.Select(key => key.Frame)];
            if (CurveData.UsesCompressedData(curve))
            {
                curve.CompressedKeyData = EncodeCompressed(curve, keys, textures);
                return;
            }

            if (curve.CurveType == AnimCurveType.StepBool && curve.KeyType == AnimCurveKeyType.Single)
            {
                curve.BoolKeys = [.. keys.Select((key, index) => key.Bool ?? throw Missing("bool", index))];
                return;
            }

            if (curve.CurveType == AnimCurveType.StepInt && curve.KeyType == AnimCurveKeyType.Single)
            {
                curve.IntKeys = [.. keys.Select((key, index) => IntOf(key, index, textures))];
                return;
            }

            var values = new List<float>(count * curve.ElementsPerKey);
            for (int index = 0; index < count; index++)
            {
                KeyDocument key = keys[index];
                values.Add(key.Value ?? throw Missing("value", index));
                if (curve.CurveType == AnimCurveType.Linear) values.Add(key.Delta ?? throw Missing("delta", index));
                if (curve.CurveType == AnimCurveType.Cubic) values.AddRange([key.C1 ?? throw Missing("c1", index), key.C2 ?? throw Missing("c2", index), key.C3 ?? throw Missing("c3", index)]);
            }

            curve.Keys = [.. values];
        }

        private static byte[] EncodeCompressed(AnimCurve curve, List<KeyDocument> keys, List<string>? textures)
        {
            int count = keys.Count;
            if (curve.CurveType == AnimCurveType.StepBool)
            {
                var bits = new byte[(count + 7) / 8];
                for (int index = 0; index < count; index++)
                {
                    if (keys[index].Bool ?? throw Missing("bool", index)) bits[index / 8] |= (byte)(1 << (index % 8));
                }

                return bits;
            }

            int width = curve.KeyType switch { AnimCurveKeyType.Int16 => 2, AnimCurveKeyType.SByte => 1, _ => 4 };
            if (curve.CurveType == AnimCurveType.StepInt)
            {
                int[] ints = [.. keys.Select((key, index) => IntOf(key, index, textures))];
                if (width < 4) return Quantize(curve, [.. ints.Select(value => (double)value)], width, whole: true);
                var bytes = new byte[count * 4];
                for (int index = 0; index < count; index++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * 4), ints[index]);
                return bytes;
            }

            if (curve.KeyType != AnimCurveKeyType.Single) return Quantize(curve, [.. keys.Select((key, index) => (double)(key.Value ?? throw Missing("value", index)))], width, whole: false);
            int perKey = curve.CurveType == AnimCurveType.Cubic ? 2 : 1;
            var floats = new byte[count * perKey * 4];
            for (int index = 0; index < count; index++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(floats.AsSpan(index * perKey * 4), keys[index].Value ?? throw Missing("value", index));
                if (perKey == 2) BinaryPrimitives.WriteSingleLittleEndian(floats.AsSpan(index * 8 + 4), keys[index].Slope ?? throw Missing("slope", index));
            }

            return floats;
        }

        private static uint Quantized(byte[] data, int index, int width)
            => width == 1 ? data[index] : BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(index * 2));

        private static double Dequantize(AnimCurve curve, uint step) => (float)(step * (double)curve.Scale + curve.Offset);

        // Narrow keys are stored as unsigned steps from the curve's bias. Values that are what the curve's own scale and
        // bias give are written back as the steps they came from; otherwise the scale and bias are chosen again to span
        // the new values, so an edited value is rounded to the nearest step (1/255 of the range for 8-bit keys).
        private static byte[] Quantize(AnimCurve curve, double[] values, int width, bool whole)
        {
            uint top = width == 1 ? byte.MaxValue : ushort.MaxValue;
            var steps = new uint[values.Length];
            bool exact = curve.Scale != 0;
            for (int index = 0; exact && index < values.Length; index++)
            {
                double step = Math.Round((values[index] - curve.Offset) / curve.Scale);
                exact = step >= 0 && step <= top && Dequantize(curve, (uint)step) is var back && (whole ? Math.Round(back) == values[index] : (float)back == (float)values[index]);
                if (exact) steps[index] = (uint)step;
            }

            if (!exact)
            {
                double low = values.Min(), high = values.Max();
                if (whole)
                {
                    if (high - low > top) throw new BfresFormatException($"The values span {high - low}, more than this curve's {width * 8}-bit keys can hold");
                    curve.Scale = 1;
                }
                else curve.Scale = high > low ? (float)((high - low) / top) : 0;
                curve.Offset = (float)low;
                for (int index = 0; index < values.Length; index++) steps[index] = curve.Scale == 0 ? 0 : (uint)Math.Clamp(Math.Round((values[index] - curve.Offset) / curve.Scale), 0, top);
            }

            var bytes = new byte[values.Length * width];
            for (int index = 0; index < values.Length; index++)
            {
                if (width == 1) bytes[index] = (byte)steps[index];
                else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(index * 2), (ushort)steps[index]);
            }

            return bytes;
        }

        private static int IntOf(KeyDocument key, int index, List<string>? textures)
            => key.Texture is { } name ? TextureIndex(name, textures) : key.Int ?? throw Missing(textures is null ? "int" : "texture", index);

        private static BfresFormatException Missing(string field, int index) => new($"Key {index} needs a {field}");
    }
}
