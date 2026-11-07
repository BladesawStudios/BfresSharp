namespace BfresSharp.Text;

/// <summary>The text form of an FMAA section: the fields of a <see cref="Model.Animation.MaterialAnim"/> as a YAML document.</summary>
/// <remarks>
/// A plain data tree that <see cref="MaterialAnimYaml"/> writes and reads. It holds every field of the section, so a
/// document read back writes the same bytes. Optional members are null when the model value is the default.
/// </remarks>
public sealed class RawAnimDocument
{
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public ushort Flags { get; set; }

    public ushort Pad { get; set; }

    public int FrameCount { get; set; }

    public uint BakedSize { get; set; }

    public ushort[] BindIndices { get; set; } = [];

    public List<string> TextureNames { get; set; } = [];

    public long[] TextureBinds { get; set; } = [];

    public List<RawItemDocument> Items { get; set; } = [];

    public List<UserDataDocument>? UserData { get; set; }
}

/// <summary>The text form of the animation of one material.</summary>
/// <remarks>The curve and constant indices are kept as read, so the entries they point to stay in place.</remarks>
public sealed class RawItemDocument
{
    public string Name { get; set; } = "";

    public ulong? NameHash { get; set; }

    public ushort ShaderParamCurveIndex { get; set; }

    public ushort TexturePatternCurveIndex { get; set; }

    public ushort VisualConstantIndex { get; set; }

    public ushort VisualCurveIndex { get; set; }

    public ushort BeginVisualConstantIndex { get; set; }

    public int InfoIndex { get; set; }

    public ushort[]? BaseDataList { get; set; }

    public List<RawParamDocument> ParamAnimInfos { get; set; } = [];

    public List<RawPatternDocument> PatternAnimInfos { get; set; } = [];

    public List<RawCurveDocument> Curves { get; set; } = [];

    public List<RawConstantDocument> Constants { get; set; } = [];
}

/// <summary>The text form of how one shader parameter is animated.</summary>
/// <remarks>Written as a single-line mapping.</remarks>
public sealed class RawParamDocument
{
    public string Name { get; set; } = "";

    public ulong? NameHash { get; set; }

    public ushort BeginCurve { get; set; }

    public ushort FloatCurveCount { get; set; }

    public ushort IntCurveCount { get; set; }

    public ushort BeginConstant { get; set; }

    public ushort ConstantCount { get; set; }

    public ushort SubBindIndex { get; set; }
}

/// <summary>The text form of how one texture slot is animated.</summary>
/// <remarks>Written as a single-line mapping.</remarks>
public sealed class RawPatternDocument
{
    public string Name { get; set; } = "";

    public ulong? NameHash { get; set; }

    public short CurveIndex { get; set; } = -1;

    public ushort BeginConstant { get; set; } = ushort.MaxValue;

    public sbyte SubBindIndex { get; set; } = -1;
}

/// <summary>The text form of a constant: where it is stored and its four bytes.</summary>
/// <remarks>
/// <see cref="Value"/> is the raw word and <see cref="Float"/> the same word as a float. Read back against the
/// constant it replaces, whichever of the two was changed is used, so either can be edited. Written as a single-line mapping.
/// </remarks>
public sealed class RawConstantDocument
{
    public uint Offset { get; set; }

    public float? Float { get; set; }

    public uint? Value { get; set; }
}

/// <summary>The text form of an animation curve.</summary>
/// <remarks>
/// The flags word is split into named parts; <see cref="ExtraFlags"/> keeps the bits none of them own. Only the
/// key list the curve's type uses is written. A compressed curve's keys are a hex string, as the game packs them.
/// </remarks>
public sealed class RawCurveDocument
{
    public string FrameType { get; set; } = "";

    public string KeyType { get; set; } = "";

    public string CurveType { get; set; } = "";

    public string PreWrap { get; set; } = "";

    public string PostWrap { get; set; } = "";

    public bool Compressed { get; set; }

    public ushort ExtraFlags { get; set; }

    public uint AnimDataOffset { get; set; }

    public float StartFrame { get; set; }

    public float EndFrame { get; set; }

    public float Scale { get; set; }

    public float Offset { get; set; }

    public float Delta { get; set; }

    public uint? CompressedHeaderWord { get; set; }

    public float[] Frames { get; set; } = [];

    public float[]? Keys { get; set; }

    public int[]? IntKeys { get; set; }

    public bool[]? BoolKeys { get; set; }

    public string? CompressedKeyData { get; set; }
}

/// <summary>The text form of a user data entry.</summary>
/// <remarks>Holds the values as text; <see cref="Type"/> says how each one is read back (numbers use the invariant culture).</remarks>
public sealed class UserDataDocument
{
    public string Name { get; set; } = "";

    public string Type { get; set; } = "";

    public List<string> Values { get; set; } = [];

    public string? Reserved { get; set; }
}
