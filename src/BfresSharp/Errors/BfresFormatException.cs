namespace BfresSharp.Errors;

/// <summary>The error thrown when bytes do not form a valid BFRES structure.</summary>
/// <remarks>
/// Carries the file offset the problem was found at when there is one. It is the base of the exceptions the
/// library throws for input it cannot use, so a caller can catch it to tell a bad file from a bug.
/// </remarks>
public class BfresFormatException : Exception
{
    public BfresFormatException(string message, long? offset = null)
        : base(offset is { } at ? $"{message} (at 0x{at:X})" : message)
    {
        Offset = offset;
    }

    public long? Offset { get; }
}
