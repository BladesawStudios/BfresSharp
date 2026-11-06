namespace BfresSharp.Errors;

/// <summary>The error thrown when bytes do not form a valid BFRES structure.</summary>
/// <remarks>
/// Carries the file offset the problem was found at when there is one. It is the only exception type the
/// library throws for malformed input, so a caller can catch it to tell a bad file from a bug.
/// </remarks>
public sealed class BfresFormatException : Exception
{
    public BfresFormatException(string message, long? offset = null)
        : base(offset is { } at ? $"{message} (at 0x{at:X})" : message)
    {
        Offset = offset;
    }

    public long? Offset { get; }
}
