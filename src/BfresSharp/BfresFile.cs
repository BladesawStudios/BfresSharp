using BfresSharp.Container;
using BfresSharp.Switch.IO;
using BfresSharp.WiiU.IO;
using McSharp;

namespace BfresSharp;

/// <summary>Reads a BFRES file of either console, unpacking the MeshCodec package Tears of the Kingdom keeps its models in.</summary>
/// <remarks>
/// The file's platform comes from its header, so a caller does not choose a reader. A package that starts with the
/// MCPK magic is decoded first, and what it holds is read as a Switch file. Writing goes through the writer of the
/// platform the file was read for.
/// </remarks>
public static class BfresFile
{
    public static Model.ResFile Read(ReadOnlyMemory<byte> data)
    {
        if (IsPackage(data.Span)) data = MeshCodec.DecompressMc(data.Span, out McStatus status) ?? throw (status == McStatus.UnsupportedVersion
                ? new Errors.BfresUnsupportedException($"The MeshCodec package version is not supported: {status}")
                : new Errors.BfresFormatException($"The MeshCodec package could not be decoded: {status}"));
        BfresHeader header = BfresHeaderReader.Read(data);
        return header.Platform == BfresPlatform.WiiU ? WiiUFileReader.Read(data) : SwitchFileReader.Read(data);
    }

    // Writes the file for the platform it was read for.
    public static byte[] Write(Model.ResFile file) => file.Platform == BfresPlatform.WiiU ? WiiUFileWriter.Write(file) : SwitchFileWriter.Write(file);

    public static void Write(Model.ResFile file, Stream destination)
    {
        if (file.Platform == BfresPlatform.WiiU) WiiUFileWriter.Write(file, destination);
        else SwitchFileWriter.Write(file, destination);
    }

    public static bool IsPackage(ReadOnlySpan<byte> data) => data.StartsWith("MCPK"u8);
}
