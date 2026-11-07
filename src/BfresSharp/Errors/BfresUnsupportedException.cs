namespace BfresSharp.Errors;

/// <summary>The error thrown when a file is well formed but uses something the library cannot read or write yet.</summary>
/// <remarks>
/// It derives from <see cref="BfresFormatException"/>, so code that catches that still catches this, and a caller that
/// wants to tell a damaged file from a missing feature can catch this first.
/// </remarks>
public sealed class BfresUnsupportedException(string message, long? offset = null) : BfresFormatException(message, offset);
