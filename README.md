# BfresSharp

Reader and writer for the Nintendo BFRES graphics archive format, on Wii U and Switch.

```csharp
BfresHeader header = BfresHeaderReader.Read(File.ReadAllBytes("model.bfres"));
Console.WriteLine($"{header.Platform} {header.Version}");
```

## Build

```bash
git submodule update --init
dotnet test BfresSharp.sln
```

Vendored libraries are submodules in `lib/vendor`: TexSharp, BntxSharp, TxtgSharp and ZstdSharp. Code conventions are in
[docs/guidelines.md](docs/guidelines.md).
