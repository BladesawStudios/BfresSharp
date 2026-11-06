namespace BfresSharp.Binary;

/// <summary>The order the bytes of a multi-byte value are stored in.</summary>
/// <remarks>Wii U files are big-endian and Switch files are little-endian, but the byte order mark in a file is what decides.</remarks>
public enum ByteOrder
{
    Little,
    Big,
}
