namespace BfresSharp.Model.Material;

/// <summary>An FMAT section: the shader, textures, samplers and parameter values used to shade shapes.</summary>
/// <remarks>
/// <see cref="Flags"/> is kept whole; <see cref="Visible"/> is its low bit. <see cref="TextureSlots"/> and
/// <see cref="SamplerSlots"/> are runtime slot tables that Switch files store zeroed. <see cref="RenderState"/> is
/// only present in Wii U files.
/// </remarks>
public sealed class Material
{
    public string Name { get; set; } = "";

    public uint Flags { get; set; } = 1;

    public ResDict<RenderInfo> RenderInfos { get; set; } = new();

    public ShaderAssign ShaderAssign { get; set; } = new();

    public List<string> TextureRefs { get; set; } = [];

    public ResDict<Sampler> Samplers { get; set; } = new();

    public ResDict<ShaderParam> ShaderParams { get; set; } = new();

    public byte[] ShaderParamData { get; set; } = [];

    // Version 10 Switch files only: the runtime lookup for each parameter and the size of the render info data block.
    public int[] ParamIndices { get; set; } = [];

    public ushort RenderInfoSize { get; set; }

    public byte[] VolatileFlags { get; set; } = [];

    public ResDict<UserData> UserData { get; set; } = new();

    public long[] TextureSlots { get; set; } = [];

    public long[] SamplerSlots { get; set; } = [];

    public RenderState? RenderState { get; set; }

    public bool Visible
    {
        get => (Flags & 1) != 0;
        set => Flags = value ? Flags | 1 : Flags & ~1u;
    }
}
