namespace BfresSharp.Model;

/// <summary>A file embedded in a BFRES archive, such as a texture archive, kept as the bytes it was stored as.</summary>
public sealed class ExternalFile
{
    public byte[] Data { get; set; } = [];
}
