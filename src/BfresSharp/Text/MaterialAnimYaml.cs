using System.Text.RegularExpressions;
using BfresSharp.Errors;
using BfresSharp.Model.Animation;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace BfresSharp.Text;

/// <summary>Writes an FMAA section as YAML and reads it back.</summary>
/// <remarks>
/// The YAML is the readable form (<see cref="AnimDocument"/>): each material lists its parameters and patterns, and each
/// of those its constants and curves, with keys as frame and value rows. A section the readable form cannot hold exactly,
/// for example one whose curves are laid out out of the usual order, is written as the field-for-field dump
/// (<see cref="RawAnimDocument"/>) instead, and the reader accepts either. A section that is written and read again writes
/// the same bytes. A document with an unknown field, a bad value or broken YAML is rejected with a
/// <see cref="BfresFormatException"/> that names the line.
/// </remarks>
public static class MaterialAnimYaml
{
    private const string Header = """
        # Material animation. Each material lists the parameters and texture patterns it animates.
        #   value / values:  a parameter held at a fixed number, or at several (a colour is red, green, blue, alpha, each 0 to 1).
        #                    The "# #RRGGBB" after it shows the colour; it is only a comment. You can also write color: "#RRGGBB" (alpha 1) or "#RRGGBBAA".
        #   animation:       the same parameter changing over time. Each key is a frame and one value per column of `offsets`.
        #   texture:         one of the names in `textures`.
        #   curves/constants: the long form, for what does not fit the above (`raw` is a plain integer; linear keys hold `delta`,
        #                    cubic keys `c1`-`c3`).
        #   8 and 16-bit curves (`keyType`) store `scale` and `bias`; edited values are rounded to the nearest step they allow.

        """;

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNewLine("\n")
        .WithEventEmitter(next => new CompactEventEmitter(next))
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer RawSerializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNewLine("\n")
        .WithEventEmitter(next => new CompactEventEmitter(next))
        .Build();

    private static readonly IDeserializer RawDeserializer = new DeserializerBuilder().Build();

    private static readonly Regex RawMarker = new(@"^FrameCount:", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <param name="anim">The section to write.</param>
    /// <param name="names">Known names by hash (see <see cref="Model.ResFile.NamesByHash"/>); a parameter named by a hash in the file is shown by its name.</param>
    public static string ToYaml(MaterialAnim anim, IReadOnlyDictionary<ulong, string>? names = null)
    {
        var table = new NameTable(names);
        try
        {
            string yaml = Header + Serializer.Serialize(AnimMapper.ToDocument(anim, table));
            if (ToRawYaml(ReadReadable(yaml, table)) == ToRawYaml(anim)) return WithSwatches(yaml);
        }
        catch (BfresFormatException)
        {
        }

        return "# This animation is laid out in a way the readable form cannot hold, so every field is shown as stored.\n" + ToRawYaml(anim);
    }

    // Next to every parameter of three or four numbers (a colour, or a direction) goes the colour they make as #RRGGBB.
    // It is a comment, which the reader ignores; a number above 1 is shown as 1 and the line says so.
    internal static string WithSwatches(string yaml)
    {
        string[] lines = yaml.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            Match match = SwatchLine.Match(lines[index]);
            if (!match.Success) continue;
            float[] parts = [.. match.Groups["numbers"].Value.Split(',').Select(text => float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float number) ? number : float.NaN)];
            if (parts.Length is not (3 or 4) || parts.Any(float.IsNaN)) continue;
            string hex = string.Concat(parts.Take(3).Select(part => ((int)Math.Round(Math.Clamp(part, 0f, 1f) * 255)).ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
            lines[index] += $"  # #{hex}" + (parts.Take(3).Any(part => part > 1f) ? " (brighter than white)" : "");
        }

        return string.Join('\n', lines);
    }

    private static readonly Regex SwatchLine = new(@"(?:values|value): \[(?<numbers>[-0-9.eE+, ]+)\]\s*(?:[,}]|$)", RegexOptions.CultureInvariant);

    // The animation being replaced tells an edited constant word from an edited float in the field-for-field form; the readable form needs no such help.
    public static MaterialAnim FromYaml(string yaml, MaterialAnim? original = null, IReadOnlyDictionary<ulong, string>? names = null)
    {
        try
        {
            if (RawMarker.IsMatch(yaml)) return FromRawYaml(yaml, original);
            return ReadReadable(yaml, new NameTable(names));
        }
        catch (YamlException error)
        {
            throw new BfresFormatException($"Line {error.Start.Line}: {error.InnerException?.Message ?? error.Message}");
        }
    }

    internal static string ToRawYaml(MaterialAnim anim) => RawSerializer.Serialize(RawAnimMapper.ToDocument(anim));

    internal static MaterialAnim FromRawYaml(string yaml, MaterialAnim? original = null)
    {
        RawAnimDocument document = RawDeserializer.Deserialize<RawAnimDocument?>(yaml) ?? throw new BfresFormatException("The YAML is empty");
        return RawAnimMapper.FromDocument(document, original);
    }

    private static MaterialAnim ReadReadable(string yaml, NameTable names)
    {
        AnimDocument document = Deserializer.Deserialize<AnimDocument?>(yaml) ?? throw new BfresFormatException("The YAML is empty");
        return AnimMapper.FromDocument(document, names);
    }

    // Puts number lists and the small records (keys, constants) on one line, which keeps long curves readable.
    internal sealed class CompactEventEmitter(IEventEmitter next) : ChainedEventEmitter(next)
    {
        private static readonly HashSet<Type> OneLineRecords =
            [typeof(RawParamDocument), typeof(RawPatternDocument), typeof(RawConstantDocument), typeof(KeyDocument), typeof(ConstantDocument), typeof(GroupKeyDocument), typeof(ModelParamDocument)];

        // A parameter held at a fixed value, or a pattern fixed to a texture, fits on one line.
        private static bool IsShort(object? value) => value switch
        {
            ParamDocument { Animation: null, Constants: null, Curves: null, IntCurves: null } parameter => parameter.Value is not null || parameter.Values is not null,
            PatternDocument { Curve: null, Constant: null } pattern => pattern.Texture is not null,
            _ => false,
        };

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            Type type = eventInfo.Source.Type;
            if (type.IsArray && type.GetElementType()!.IsPrimitive) eventInfo = new SequenceStartEventInfo(eventInfo.Source) { Style = SequenceStyle.Flow };
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            if (OneLineRecords.Contains(eventInfo.Source.Type) || IsShort(eventInfo.Source.Value)) eventInfo = new MappingStartEventInfo(eventInfo.Source) { Style = MappingStyle.Flow };
            base.Emit(eventInfo, emitter);
        }
    }
}
