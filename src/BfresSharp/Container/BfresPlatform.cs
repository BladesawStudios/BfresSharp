namespace BfresSharp.Container;

/// <summary>The console a BFRES file was made for.</summary>
/// <remarks>The two platforms use different header layouts, version encodings and pointer widths.</remarks>
public enum BfresPlatform
{
    WiiU,
    Switch,
}
