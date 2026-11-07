# TODO

## Animations

- Scene animations (camera, light, fog and the scene animation that holds them).
  - Switch: the old BfresLibrary has full parsers (`Switch/SceneAnim/*Parser.cs`) and a text converter (`TextConvert/SceneAnimConvert.cs`), so these are needed for parity. Replace the `RequireEmpty` in `SwitchFileReader` and write the root scene count.
  - Wii U: the old library only reads the dictionary and count, never the camera, light or fog data. Do this only once a corpus file turns up to check it against. Replace the `RequireEmpty` in `WiiUFileReader` (offset 0x48).
- Wii U shape animations (FSHA). The BotW sample has none, so they cannot be verified yet. Replace the `RequireEmpty` in `WiiUFileReader` (offset 0x44).

## Wii U

- Run wider strides and other Wii U titles, and re-run the Switch corpus after the Wii U changes.

## Later

- Platform converters (Wii U to Switch and back).
- TextConvert JSON and text.
- Helpers, `Half` and `Decimal10x5`.
- Embedded textures through BntxSharp, TexSharp and TxtgSharp.
- Performance optimisation.
- Split `ShaderInfoCodec.cs` (over 256 lines).
- Full xunit suite.
