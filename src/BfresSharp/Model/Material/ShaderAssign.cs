namespace BfresSharp.Model.Material;

/// <summary>Which shader a material uses and how its attributes, samplers and options map onto that shader.</summary>
/// <remarks>
/// In version 10 Switch files an entry the material leaves at the shader's default is stored as
/// <see cref="DefaultValue"/> rather than a value.
/// </remarks>
public sealed class ShaderAssign
{
    public const string DefaultValue = "<Default Value>";

    public string ShaderArchiveName { get; set; } = "";

    public string ShadingModelName { get; set; } = "";

    public uint Revision { get; set; }

    public ResDict<string> AttribAssigns { get; set; } = new();

    public ResDict<string> SamplerAssigns { get; set; } = new();

    public ResDict<string> ShaderOptions { get; set; } = new();
}
