namespace BfresSharp.Container;

/// <summary>The console a BFRES file was made for.</summary>
/// <remarks>
/// Wii U files are big-endian with 32-bit offsets. Switch and Switch 2 files are little-endian with 64-bit pointers and a
/// relocation table; Switch 2 files are version 10.4 and later. The header holds no console field, so Switch 2 is
/// recognised by that version.
/// </remarks>
public enum BfresPlatform
{
    WiiU,
    Switch,
    Switch2,
}
