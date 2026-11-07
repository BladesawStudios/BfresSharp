# TODO

## Animations

- Scene animations (camera, light, fog and the scene animation that holds them).
  - Switch: the old BfresLibrary has full parsers (`Switch/SceneAnim/*Parser.cs`) and a text converter (`TextConvert/SceneAnimConvert.cs`), so these are needed for parity. Replace the `RequireEmpty` in `SwitchFileReader` and write the root scene count.
  - Wii U: the old library only reads the dictionary and count, never the camera, light or fog data. Do this only once a corpus file turns up to check it against. Replace the `RequireEmpty` in `WiiUFileReader` (offset 0x48).
- Wii U shape animations (FSHA). The BotW sample has none, so they cannot be verified yet. Replace the `RequireEmpty` in `WiiUFileReader` (offset 0x44).

## Switch 2 MeshCodec packages

- McSharp rejects the Switch 2 `.bfres.mc` files (package version 1.0.0, `UnsupportedVersion`), so `BfresFile.Read` cannot open them. The Ghidra project of the Switch 1 executable only has the version 0.1 decoder (`nn::mc::Decompress` reads an FMSH header of 0x22 bytes), so the new format needs the Switch 2 executable.
- What the files show: the package header is `MCPK`, bytes `00 00 01 00`, then the same size word. Four words follow instead of the zstd frames (the second `FMSH` header starts at 0x1C, after a compressed size, a size of 2 MB or so, the stream size and `0x803`). The FMSH header has version `0x10000`, the work memory size, two sizes that are multiples of 0x100 (0x9A500 and 0x33C500 in `Npc_Gerudo_GEN_001`), the constant `0x129` and then data that is not the old comp header. Five such files sit in `Switch2/OOTOfTime/*/romfs/Model`.

- A raw byte search of the Switch 2 executable for `MCPK` and `FMSH` finds nothing, and the binary has no MeshCodec or zstd strings, so the decoder is probably not in that module (hardware decompression, or another module). The payload is high entropy (about 7.99 bits a byte) with no zstd magic, like version 0, so it is still compressed. Without the decoder the format cannot be ported; try the system modules or a memory dump of a running game.

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
