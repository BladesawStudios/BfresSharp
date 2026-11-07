namespace BfresSharp.Gx2.Addressing;

/// <summary>The properties of the sixteen address-library tile modes.</summary>
/// <remarks>
/// A tile mode is numbered as <see cref="GX2TileMode"/> numbers it from 1 on, so mode 0 is the linear general mode the
/// address library has and mode 16 is the special linear mode of GX2, which the layout code turns into mode 0. Modes 2
/// and 3 are micro tiled, 4 to 15 are macro tiled, and every mode is either thin (one slice per micro tile) or thick
/// (four or eight).
/// </remarks>
internal static class Gx2TileModes
{
    public const uint LinearGeneral = 0;
    public const uint LinearAligned = 1;
    public const uint Micro1dThin = 2;
    public const uint Micro1dThick = 3;
    public const uint Macro2dThin1 = 4;
    public const uint Macro2dThin2 = 5;
    public const uint Macro2dThin4 = 6;
    public const uint Macro2dThick = 7;
    public const uint Macro2bThin1 = 8;
    public const uint Macro2bThin2 = 9;
    public const uint Macro2bThin4 = 10;
    public const uint Macro2bThick = 11;
    public const uint Macro3dThin1 = 12;
    public const uint Macro3dThick = 13;
    public const uint Macro3bThin1 = 14;
    public const uint Macro3bThick = 15;

    public static bool IsLinear(uint mode) => mode <= LinearAligned;

    public static bool IsMicroTiled(uint mode) => mode is Micro1dThin or Micro1dThick;

    public static uint Thickness(uint mode) => mode is Micro1dThick or Macro2dThick or Macro2bThick or Macro3dThick or Macro3bThick ? 4u : 1u;

    public static bool IsThickMacroTiled(uint mode) => mode is Macro2dThick or Macro2bThick or Macro3dThick or Macro3bThick;

    public static bool IsBankSwapped(uint mode) => mode is >= Macro2bThin1 and <= Macro2bThick or Macro3bThin1 or Macro3bThick;

    public static uint AspectRatio(uint mode) => mode switch
    {
        Macro2dThin2 or Macro2bThin2 => 2,
        Macro2dThin4 or Macro2bThin4 => 4,
        _ => 1,
    };

    // How far the bank and pipe pattern turns from one slice to the next.
    public static uint Rotation(uint mode) => mode switch
    {
        Macro2dThin1 or Macro2dThin2 or Macro2dThin4 or Macro2dThick or Macro2bThin1 or Macro2bThin2 or Macro2bThin4 or Macro2bThick => 2,
        Macro3dThin1 or Macro3dThick or Macro3bThin1 or Macro3bThick => 1,
        _ => 0,
    };

    public static uint ToNonBankSwapped(uint mode) => mode switch
    {
        Macro2bThin1 => Macro2dThin1,
        Macro2bThin2 => Macro2dThin2,
        Macro2bThin4 => Macro2dThin4,
        Macro2bThick => Macro2dThick,
        Macro3bThin1 => Macro3dThin1,
        Macro3bThick => Macro3dThick,
        _ => mode,
    };

    // The width and height in micro tiles' pixels of one macro tile.
    public static (uint Width, uint Height) MacroTileSize(uint mode)
    {
        uint ratio = AspectRatio(mode);
        return (32 / ratio, 16 * ratio);
    }
}
