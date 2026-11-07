namespace BfresSharp.Model.Animation;

/// <summary>A curve of keyed values over frames that drives one animated value.</summary>
/// <remarks>
/// The flags word packs the frame type, key type, curve type and wrap modes; the properties read and write the
/// bits they own. Float curves keep their keys in <see cref="Keys"/> as one row per key (four coefficients for
/// cubic, two for linear, one otherwise). Integer step curves keep <see cref="IntKeys"/> and boolean step curves keep
/// <see cref="BoolKeys"/>, so no value passes through a float. A curve with the compressed flag, which Switch 2 files
/// use, keeps its key bytes untouched in <see cref="CompressedKeyData"/> so it writes back exactly.
/// </remarks>
public sealed class AnimCurve
{
    public const ushort CompressedFlag = 0x4000;

    private const ushort FrameTypeMask = 0b11;
    private const ushort KeyTypeMask = 0b1100;
    private const ushort CurveTypeMask = 0b111_0000;

    public ushort Flags { get; set; }

    public AnimCurveFrameType FrameType
    {
        get => (AnimCurveFrameType)(Flags & FrameTypeMask);
        set => Flags = (ushort)((Flags & ~FrameTypeMask) | (ushort)value);
    }

    public AnimCurveKeyType KeyType
    {
        get => (AnimCurveKeyType)(Flags & KeyTypeMask);
        set => Flags = (ushort)((Flags & ~KeyTypeMask) | (ushort)value);
    }

    public AnimCurveType CurveType
    {
        get => (AnimCurveType)(Flags & CurveTypeMask);
        set => Flags = (ushort)((Flags & ~CurveTypeMask) | (ushort)value);
    }

    public WrapMode PreWrap
    {
        get => (WrapMode)((Flags >> 8) & 3);
        set => Flags = (ushort)((Flags & ~(3 << 8)) | ((ushort)value << 8));
    }

    public WrapMode PostWrap
    {
        get => (WrapMode)((Flags >> 12) & 3);
        set => Flags = (ushort)((Flags & ~(3 << 12)) | ((ushort)value << 12));
    }

    public bool IsCompressed => (Flags & CompressedFlag) != 0;

    public uint AnimDataOffset { get; set; }

    public float StartFrame { get; set; }

    public float EndFrame { get; set; }

    public float Scale { get; set; }

    public float Offset { get; set; }

    public float Delta { get; set; }

    // The word of a compressed curve that sits before the scale and offset; nothing reads it.
    public uint CompressedHeaderWord { get; set; }

    public float[] Frames { get; set; } = [];

    public float[,] Keys { get; set; } = new float[0, 0];

    public int[] IntKeys { get; set; } = [];

    public bool[] BoolKeys { get; set; } = [];

    public byte[] CompressedKeyData { get; set; } = [];

    public int ElementsPerKey => CurveType switch
    {
        AnimCurveType.Cubic => 4,
        AnimCurveType.Linear => 2,
        _ => 1,
    };
}
