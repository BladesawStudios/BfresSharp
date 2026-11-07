namespace BfresSharp.Model;

/// <summary>A file embedded in a BFRES archive, such as a texture archive, kept as the bytes it was stored as.</summary>
public sealed class ExternalFile
{
    public ReadOnlyMemory<byte> Data { get; set; }

    // Where a Wii U file kept the data, or -1; kept so that a file whose data overlaps or sits at a fixed place writes back the same.
    public int DataPosition { get; set; } = -1;
}
