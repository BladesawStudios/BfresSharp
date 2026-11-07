namespace BfresSharp.Model.Animation;

/// <summary>An FMAA section: the animation of shader parameters and texture patterns of the materials of a model.</summary>
/// <remarks>
/// Holds one <see cref="MaterialAnimItem"/> for each animated material, selected by <see cref="BindIndices"/>.
/// <see cref="TextureNames"/> lists the textures patterns choose from, and <see cref="TextureBinds"/> holds the runtime
/// handles stored with them (-1 when unbound). <see cref="Pad"/> keeps the unused word of version 9 and later headers.
/// </remarks>
public sealed class MaterialAnim
{
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public ushort Flags { get; set; }

    public ushort Pad { get; set; }

    public int FrameCount { get; set; }

    public uint BakedSize { get; set; }

    public ushort[] BindIndices { get; set; } = [];

    public List<MaterialAnimItem> Items { get; set; } = [];

    public List<string> TextureNames { get; set; } = [];

    public long[] TextureBinds { get; set; } = [];

    public ResDict<UserData> UserData { get; set; } = new();

    public bool Baked => (Flags & 1) != 0;

    public bool Loop => (Flags & 4) != 0;
}
