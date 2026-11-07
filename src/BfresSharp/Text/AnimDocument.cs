namespace BfresSharp.Text;

/// <summary>The readable text form of an FMAA section: what is animated and how, without the file's index bookkeeping.</summary>
/// <remarks>
/// Parameters, patterns and their curves are nested under the material they belong to, so the indices and counts the
/// binary keeps (where a material's curves start, how many each entry has) are worked out when the text is read back.
/// A value that differs from what that would give is written out as an explicit override, so nothing is lost.
/// </remarks>
public sealed class AnimDocument
{
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public int Frames { get; set; }

    public bool Loop { get; set; }

    public bool Baked { get; set; }

    public ushort? OtherFlags { get; set; }

    public ushort? Pad { get; set; }

    public uint? BakedSize { get; set; }

    public ushort[] BindIndices { get; set; } = [];

    public List<string> Textures { get; set; } = [];

    public long[]? TextureBinds { get; set; }

    public List<MaterialDocument> Materials { get; set; } = [];

    public List<UserDataDocument>? UserData { get; set; }
}

/// <summary>The animation of one material.</summary>
public sealed class MaterialDocument
{
    public string? Material { get; set; }

    public List<ParamDocument>? Params { get; set; }

    public List<PatternDocument>? Patterns { get; set; }

    public VisibilityDocument? Visibility { get; set; }

    public IndicesDocument? Indices { get; set; }

    public int? InfoIndex { get; set; }

    public ushort[]? BaseData { get; set; }
}

/// <summary>A shader parameter and the constants and curves that drive its components.</summary>
public sealed class ParamDocument
{
    public string? Name { get; set; }

    public ushort? SubBind { get; set; }

    // A parameter held at one fixed value: a single number, or the numbers of its components in order (a colour is red, green, blue, alpha).
    public float? Value { get; set; }

    public float[]? Values { get; set; }

    // Read only: a colour as "#RRGGBB" or "#RRGGBBAA", for typing a colour in by hand.
    public string? Color { get; set; }

    // The components of a parameter that change over time, all on the same frames.
    public CurveGroupDocument? Animation { get; set; }

    public List<ConstantDocument>? Constants { get; set; }

    public List<CurveDocument>? Curves { get; set; }

    public List<CurveDocument>? IntCurves { get; set; }

    public ushort? BeginCurve { get; set; }

    public ushort? BeginConstant { get; set; }
}

/// <summary>A texture slot and the curve or constant that picks its texture.</summary>
public sealed class PatternDocument
{
    public string? Name { get; set; }

    public sbyte? SubBind { get; set; }

    // A pattern fixed to one texture.
    public string? Texture { get; set; }

    public CurveDocument? Curve { get; set; }

    public ConstantDocument? Constant { get; set; }

    public short? CurveIndex { get; set; }

    public ushort? BeginConstant { get; set; }
}

/// <summary>The curves and constants of a material that no parameter or pattern claims; they animate visibility.</summary>
public sealed class VisibilityDocument
{
    public List<CurveDocument>? Curves { get; set; }

    public List<ConstantDocument>? Constants { get; set; }
}

/// <summary>The index words of a material, written only when they are not what the entries give.</summary>
public sealed class IndicesDocument
{
    public ushort ShaderParamCurve { get; set; }

    public ushort TexturePatternCurve { get; set; }

    public ushort VisualConstant { get; set; }

    public ushort VisualCurve { get; set; }

    public ushort BeginVisualConstant { get; set; }
}

/// <summary>A value set without a curve: the field it goes to and the value.</summary>
/// <remarks>
/// A word that is a sensible float is written as <see cref="Value"/>; any other word as <see cref="Raw"/>, a signed
/// integer. A texture index in a pattern is written as the <see cref="Texture"/> name. Written as a single-line mapping.
/// </remarks>
public sealed class ConstantDocument
{
    public uint? Offset { get; set; }

    public float? Value { get; set; }

    public int? Raw { get; set; }

    public string? Texture { get; set; }
}

/// <summary>An animation curve: its keys and how they are read.</summary>
public sealed class CurveDocument
{
    public uint? Offset { get; set; }

    public string Type { get; set; } = "";

    public string? KeyType { get; set; }

    public string? FrameType { get; set; }

    public string? PreWrap { get; set; }

    public string? PostWrap { get; set; }

    public float Start { get; set; }

    public float End { get; set; }

    public float? Scale { get; set; }

    public float? Bias { get; set; }

    public float? Delta { get; set; }

    public bool? Compressed { get; set; }

    public ushort? OtherFlags { get; set; }

    public uint? HeaderWord { get; set; }

    public List<KeyDocument>? Keys { get; set; }

    public float[]? Frames { get; set; }

    public float[]? Values { get; set; }

    public int[]? IntValues { get; set; }

    public bool[]? BoolValues { get; set; }

    public string? Data { get; set; }
}

/// <summary>One key of a curve: the frame and the value there. Written as a single-line mapping.</summary>
/// <remarks>
/// Linear curves also hold the <see cref="Delta"/> to the next key; cubic curves hold <see cref="C1"/> to
/// <see cref="C3"/> (or <see cref="Slope"/> when compressed). Step curves hold <see cref="Int"/>, <see cref="Bool"/> or <see cref="Texture"/>.
/// </remarks>
public sealed class KeyDocument
{
    public float Frame { get; set; }

    public float? Value { get; set; }

    public float? Slope { get; set; }

    public float? Delta { get; set; }

    public float? C1 { get; set; }

    public float? C2 { get; set; }

    public float? C3 { get; set; }

    public int? Int { get; set; }

    public bool? Bool { get; set; }

    public string? Texture { get; set; }
}

/// <summary>Several curves that share everything but their values, shown as one table: a colour changing over time.</summary>
/// <remarks>
/// <see cref="Offsets"/> says which component each column of the keys' values belongs to. Everything else is the shared
/// description of the curves (see <see cref="CurveDocument"/>); each column becomes a curve of its own when read back.
/// </remarks>
public sealed class CurveGroupDocument
{
    public uint[] Offsets { get; set; } = [];

    public string Type { get; set; } = "";

    public string? KeyType { get; set; }

    public string? FrameType { get; set; }

    public string? PreWrap { get; set; }

    public string? PostWrap { get; set; }

    public float Start { get; set; }

    public float End { get; set; }

    public float? Scale { get; set; }

    public float? Bias { get; set; }

    public float? Delta { get; set; }

    public bool? Compressed { get; set; }

    public ushort? OtherFlags { get; set; }

    public uint? HeaderWord { get; set; }

    public List<GroupKeyDocument> Keys { get; set; } = [];
}

/// <summary>One key of a curve group: the frame and the value of each component there. Written as a single-line mapping.</summary>
public sealed class GroupKeyDocument
{
    public float Frame { get; set; }

    public float[] Value { get; set; } = [];

    public float[]? Slope { get; set; }
}
