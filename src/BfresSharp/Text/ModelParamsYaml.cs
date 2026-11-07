using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BfresSharp.Text;

/// <summary>The shader parameters of a model's materials as YAML: the colours and numbers a material is drawn with.</summary>
/// <remarks>
/// Each material lists its parameters by name with their current values. <see cref="Apply"/> reads the text back and
/// writes the values that changed into the model; a parameter left out of the text keeps its value, and one whose
/// numbers were not changed keeps its exact bytes. Names the file holds as hashes are shown by name when a table is given.
/// </remarks>
public static class ModelParamsYaml
{
    private const string Header = """
        # Shader parameters of the model's materials, as stored in the model. Edit the numbers; leave a line out to keep it as it is.
        #   values:  the components of a parameter in order. A colour is red, green, blue, alpha, each 0 to 1 (more than 1 is brighter than white).
        #            The "# #RRGGBB" after it shows the colour; it is only a comment. You can also write color: "#RRGGBB" or "#RRGGBBAA".
        #   ints:    whole numbers, for the int and bool types. `type` is shown for information and is not read.
        # Animations (the other files in a BFRES) can override these while the game runs.

        """;

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNewLine("\n")
        .WithEventEmitter(next => new MaterialAnimYaml.CompactEventEmitter(next))
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <param name="model">The model whose materials are written.</param>
    /// <param name="names">Known names by hash.</param>
    /// <param name="bigEndian">Whether the file stores its numbers big endian.</param>
    /// <param name="onlyMaterial">When given, only that material is written (a file per material); the text still applies with <see cref="Apply"/>.</param>
    public static string ToYaml(ResModel model, IReadOnlyDictionary<ulong, string>? names = null, bool bigEndian = false, string? onlyMaterial = null)
    {
        var table = new NameTable(names);
        var document = new ModelDocument
        {
            Name = model.Name,
            Materials = [.. model.Materials.Where(entry => onlyMaterial is null || entry.Key == onlyMaterial).Select(entry => new MaterialParamsDocument
            {
                Material = entry.Key,
                Params = [.. entry.Value.ShaderParams.Values.Select(param => ToDocument(entry.Value, param, table, bigEndian))],
            })],
        };
        return MaterialAnimYaml.WithSwatches(Header + Serializer.Serialize(document));
    }

    /// <summary>Checks that the text is a valid parameter document, without a model to apply it to.</summary>
    public static void Check(string yaml) => Read(yaml);

    public static void Apply(ResModel model, string yaml, IReadOnlyDictionary<ulong, string>? names = null, bool bigEndian = false)
    {
        var table = new NameTable(names);
        foreach (MaterialParamsDocument materialText in Read(yaml).Materials)
        {
            string key = materialText.Material ?? throw new BfresFormatException("A material needs a name");
            if (!model.Materials.ContainsKey(key)) throw new BfresFormatException($"The model has no material \"{key}\"; it has: {string.Join(", ", model.Materials.Keys)}");
            Material material = model.Materials[key];

            // Parameters with the same name are matched in the order they are listed.
            var remaining = new Dictionary<string, Queue<ShaderParam>>(StringComparer.Ordinal);
            foreach (ShaderParam param in material.ShaderParams.Values)
            {
                string name = table.Show(param.Name, param.NameHash) ?? "";
                if (!remaining.TryGetValue(name, out Queue<ShaderParam>? queue)) remaining[name] = queue = new Queue<ShaderParam>();
                queue.Enqueue(param);
            }

            foreach (ModelParamDocument paramText in materialText.Params ?? [])
            {
                string name = paramText.Name ?? throw new BfresFormatException($"A parameter of {key} needs a name");
                if (!remaining.TryGetValue(name, out Queue<ShaderParam>? queue) || queue.Count == 0) throw new BfresFormatException($"{key} has no parameter \"{name}\" (or it is listed more times than it exists)");
                Write(material, queue.Dequeue(), paramText, name, key, bigEndian);
            }
        }
    }

    private static ModelParamDocument ToDocument(Material material, ShaderParam param, NameTable names, bool bigEndian)
    {
        var document = new ModelParamDocument { Name = names.Show(param.Name, param.NameHash), Type = param.Type.ToString() };
        if (ShaderParamValues.IsFloat(param.Type))
        {
            float[] values = ShaderParamValues.ReadFloats(material, param, bigEndian);
            if (values.Length == 1) document.Value = values[0];
            else document.Values = values;
        }
        else
        {
            int[] values = ShaderParamValues.ReadInts(material, param, bigEndian);
            if (values.Length == 1) document.Int = values[0];
            else document.Ints = values;
        }

        return document;
    }

    private static void Write(Material material, ShaderParam param, ModelParamDocument text, string name, string materialName, bool bigEndian)
    {
        int count = param.DataSize / 4;
        if (ShaderParamValues.IsFloat(param.Type))
        {
            if (text.Int is not null || text.Ints is not null) throw new BfresFormatException($"{materialName}.{name} is a {param.Type}: write its numbers as value or values, not ints");
            float[] values = text.Values ?? (text.Value is { } single ? [single] : text.Color is { } color ? ParseColor(color) : throw new BfresFormatException($"{materialName}.{name} needs value or values"));
            if (values.Length != count) throw new BfresFormatException($"{materialName}.{name} is a {param.Type} and takes {count} number(s), but has {values.Length}");
            float[] current = ShaderParamValues.ReadFloats(material, param, bigEndian);
            if (!values.Select(BitConverter.SingleToUInt32Bits).SequenceEqual(current.Select(BitConverter.SingleToUInt32Bits))) ShaderParamValues.WriteFloats(material, param, values, bigEndian);
            return;
        }

        if (text.Value is not null || text.Values is not null || text.Color is not null) throw new BfresFormatException($"{materialName}.{name} is a {param.Type}: write its numbers as int or ints, not values");
        int[] ints = text.Ints ?? (text.Int is { } one ? [one] : throw new BfresFormatException($"{materialName}.{name} needs int or ints"));
        if (ints.Length != count) throw new BfresFormatException($"{materialName}.{name} is a {param.Type} and takes {count} number(s), but has {ints.Length}");
        if (!ints.SequenceEqual(ShaderParamValues.ReadInts(material, param, bigEndian))) ShaderParamValues.WriteInts(material, param, ints, bigEndian);
    }

    private static ModelDocument Read(string yaml)
    {
        try
        {
            return Deserializer.Deserialize<ModelDocument?>(yaml) ?? throw new BfresFormatException("The YAML is empty");
        }
        catch (YamlException error)
        {
            throw new BfresFormatException($"Line {error.Start.Line}: {error.InnerException?.Message ?? error.Message}");
        }
    }

    // "#RRGGBB" or "#RRGGBBAA" as four floats from 0 to 1; without the last pair the alpha is 1.
    private static float[] ParseColor(string text)
    {
        string digits = text.TrimStart('#');
        if (digits.Length is not (6 or 8) || !digits.All(Uri.IsHexDigit)) throw new BfresFormatException($"Color \"{text}\" is not #RRGGBB or #RRGGBBAA");
        float[] channels = [.. Enumerable.Range(0, digits.Length / 2).Select(index => Convert.ToInt32(digits.Substring(index * 2, 2), 16) / 255f)];
        return channels.Length == 4 ? channels : [.. channels, 1f];
    }
}

/// <summary>The text form of a model's material parameters.</summary>
public sealed class ModelDocument
{
    public string Name { get; set; } = "";

    public List<MaterialParamsDocument> Materials { get; set; } = [];
}

/// <summary>The parameters of one material.</summary>
public sealed class MaterialParamsDocument
{
    public string? Material { get; set; }

    public List<ModelParamDocument>? Params { get; set; }
}

/// <summary>One shader parameter and its value. Written as a single-line mapping.</summary>
public sealed class ModelParamDocument
{
    public string? Name { get; set; }

    public string? Type { get; set; }

    public float? Value { get; set; }

    public float[]? Values { get; set; }

    public int? Int { get; set; }

    public int[]? Ints { get; set; }

    // Read only: a colour as "#RRGGBB" or "#RRGGBBAA", for typing a colour in by hand.
    public string? Color { get; set; }
}
