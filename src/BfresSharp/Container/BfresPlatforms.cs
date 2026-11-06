namespace BfresSharp.Container;

/// <summary>Questions about a <see cref="BfresPlatform"/>.</summary>
/// <remarks>Keeps the "is this a Switch family file" test in one place so no reader repeats the comparison.</remarks>
public static class BfresPlatforms
{
    public static bool UsesSwitchLayout(this BfresPlatform platform) => platform != BfresPlatform.WiiU;

    // Version 10.4 is the first the Switch 2 writes; the header stores no console.
    public static BfresPlatform ForSwitchVersion(uint raw)
    {
        return raw >= SwitchVersion104 ? BfresPlatform.Switch2 : BfresPlatform.Switch;
    }

    private const uint SwitchVersion104 = 0x000A0400;
}
