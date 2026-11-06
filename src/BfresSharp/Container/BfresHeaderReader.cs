using BfresSharp.Binary;
using BfresSharp.Errors;

namespace BfresSharp.Container;

/// <summary>Reads and validates the header at the start of a BFRES file.</summary>
/// <remarks>
/// A Switch file starts with "FRES" and four spaces, then the version, the byte order mark, the alignment
/// shift and the file size. A Wii U file starts with "FRES" and the version directly, then the byte order
/// mark, the file size and the alignment. The two are told apart by the four spaces, which no Wii U version can
/// be. <see cref="TryRead"/> never throws for bad input; <see cref="Read"/> throws
/// <see cref="BfresFormatException"/> with the reason.
/// </remarks>
public static class BfresHeaderReader
{
    private const int SwitchHeaderLength = 0x20;
    private const int WiiUHeaderLength = 0x14;
    private const int MaxAlignmentShift = 31;

    public static BfresHeader Read(ReadOnlyMemory<byte> data)
    {
        return Parse(data, out string? error) ?? throw new BfresFormatException(error!);
    }

    public static bool TryRead(ReadOnlyMemory<byte> data, out BfresHeader header)
    {
        header = Parse(data, out _)!;
        return header is not null;
    }

    private static BfresHeader? Parse(ReadOnlyMemory<byte> data, out string? error)
    {
        error = null;
        var view = new ByteView(data, ByteOrder.Little);
        if (!view.StartsWith(0, "FRES"u8))
        {
            error = "Data does not start with the FRES magic";
            return null;
        }

        return view.StartsWith(4, "    "u8) ? ParseSwitch(view, out error) : ParseWiiU(view, out error);
    }

    private static BfresHeader? ParseSwitch(ByteView view, out string? error)
    {
        error = null;
        if (view.Length < SwitchHeaderLength)
            return Fail($"A Switch header needs {SwitchHeaderLength} bytes, found {view.Length}", out error);
        if (!TryReadByteOrder(view, 0xC, out ByteOrder order))
            return Fail("Switch byte order mark is not FF FE or FE FF", out error);

        view = view.WithOrder(order);
        int shift = view.U8(0xE);
        if (shift > MaxAlignmentShift)
            return Fail($"Alignment shift {shift} is out of range", out error);

        uint rawVersion = view.U32(8);
        BfresPlatform platform = BfresPlatforms.ForSwitchVersion(rawVersion);
        var version = new BfresVersion(platform, rawVersion);
        return new BfresHeader(platform, order, version, view.U32(0x1C), 1u << shift);
    }

    private static BfresHeader? ParseWiiU(ByteView view, out string? error)
    {
        error = null;
        if (view.Length < WiiUHeaderLength)
            return Fail($"A Wii U header needs {WiiUHeaderLength} bytes, found {view.Length}", out error);
        if (!TryReadByteOrder(view, 8, out ByteOrder order))
            return Fail("Wii U byte order mark is not FF FE or FE FF", out error);

        view = view.WithOrder(order);
        var version = new BfresVersion(BfresPlatform.WiiU, view.U32(4));
        return new BfresHeader(BfresPlatform.WiiU, order, version, view.U32(0xC), view.U32(0x10));
    }

    // The mark is the two bytes of 0xFEFF in the file's own order, so FF FE is little-endian and FE FF is big-endian.
    private static bool TryReadByteOrder(ByteView view, long offset, out ByteOrder order)
    {
        order = ByteOrder.Little;
        if (view.StartsWith(offset, [0xFF, 0xFE])) return true;
        order = ByteOrder.Big;
        return view.StartsWith(offset, [0xFE, 0xFF]);
    }

    private static BfresHeader? Fail(string message, out string? error)
    {
        error = message;
        return null;
    }
}
