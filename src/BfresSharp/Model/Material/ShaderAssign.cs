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

    // How the file lists the chosen names of each group; kept so that writing reproduces it.
    public ChoiceLayout? AttribLayout { get; set; }

    public ChoiceLayout? SamplerLayout { get; set; }

    public ChoiceLayout? OptionLayout { get; set; }
}

/// <summary>The names a file lists for one group of shader choices and the name each key takes (-1 for the default).</summary>
/// <remarks>For options, the first <see cref="Switches"/> names are the switches, kept as bits in the file.</remarks>
public sealed record ChoiceLayout(IReadOnlyList<string> Names, IReadOnlyList<int> Slots, int Switches = 0)
{
    // Whether the layout still describes these values, key by key.
    public bool Matches(IReadOnlyList<string> values)
    {
        if (values.Count != Slots.Count) return false;
        for (int index = 0; index < values.Count; index++)
        {
            int slot = Slots[index];
            if (slot >= Names.Count || (slot < 0 ? values[index] != ShaderAssign.DefaultValue : values[index] != Names[slot])) return false;
        }

        return true;
    }
}
