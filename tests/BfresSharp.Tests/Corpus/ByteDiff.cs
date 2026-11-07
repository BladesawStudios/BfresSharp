namespace BfresSharp.Tests.Corpus;

/// <summary>Describes where two byte arrays first differ, for round trip failures.</summary>
/// <remarks>Shows the offset and a few bytes around it from each array so a failure points at the structure that was written wrong.</remarks>
internal static class ByteDiff
{
    internal static string? Describe(byte[] expected, byte[] actual)
    {
        int length = Math.Min(expected.Length, actual.Length);
        int at = 0;
        while (at < length && expected[at] == actual[at]) at++;
        if (at == length && expected.Length == actual.Length) return null;
        return $"first difference at 0x{at:X} (expected size {expected.Length}, got {actual.Length}); expected {Around(expected, at)} got {Around(actual, at)}";
    }

    private static string Around(byte[] data, int at)
    {
        int start = Math.Max(0, at - 4);
        return Convert.ToHexString(data.AsSpan(start, Math.Min(16, data.Length - start)));
    }
}
