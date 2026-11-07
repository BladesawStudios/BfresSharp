using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>The shader assignment records of a version 10 model: one for each distinct assignment its materials use.</summary>
/// <remarks>
/// Materials that name the same archive and shading model and list the same render infos, parameters, attributes,
/// samplers and options share a record, which the model stores once. The set writes the records together and, when the
/// first material that uses a record writes its body, the lists and dictionaries the record points to.
/// </remarks>
public sealed class ShaderAssignSet
{
    private readonly List<Material> _representatives = [];
    private readonly Dictionary<Material, int> _groups = new(ReferenceEqualityComparer.Instance);
    private readonly List<ShaderAssignV10Codec.Slots> _slots = [];
    private readonly List<int> _positions = [];
    private readonly HashSet<int> _written = [];

    public ShaderAssignSet(IEnumerable<Material> materials)
    {
        var bySignature = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Material material in materials)
        {
            string signature = Signature(material);
            if (!bySignature.TryGetValue(signature, out int group))
            {
                bySignature[signature] = group = _representatives.Count;
                _representatives.Add(material);
            }

            _groups[material] = group;
        }
    }

    public int Count => _representatives.Count;

    public int GroupOf(Material material) => _groups[material];

    public int PositionOf(Material material) => _positions[_groups[material]];

    // Writes the records one after another at the current position.
    public void WriteRecords(SwitchWriter writer)
    {
        foreach (Material material in _representatives)
        {
            _positions.Add(writer.Position);
            _slots.Add(ShaderAssignV10Codec.WriteRecord(writer, material));
        }
    }

    // Writes what the record of the material's group points to, unless an earlier material already did.
    public void WriteBodyOnce(SwitchWriter writer, Material material)
    {
        int group = _groups[material];
        if (!_written.Add(group)) return;
        ShaderAssignV10Codec.WriteBody(writer, _representatives[group], _slots[group]);
    }

    private static string Signature(Material material)
    {
        ShaderAssign assign = material.ShaderAssign;
        var parts = new List<string> { assign.ShaderArchiveName, assign.ShadingModelName };
        parts.AddRange(material.RenderInfos.Select((entry, index) => $"{material.RenderInfos.HashOf(index)?.ToString() ?? entry.Key}:{entry.Value.Type}"));
        parts.Add("|");
        parts.AddRange(material.ShaderParams.Select((entry, index) => $"{material.ShaderParams.HashOf(index)?.ToString() ?? entry.Key}:{entry.Value.DataOffset}:{entry.Value.Type}"));
        parts.Add("|");
        parts.AddRange(assign.AttribAssigns.Keys);
        parts.Add("|");
        parts.AddRange(assign.SamplerAssigns.Keys);
        parts.Add("|");
        parts.AddRange(assign.ShaderOptions.Keys);
        parts.Add(material.ShaderParamData.Length.ToString());
        return string.Join('\0', parts);
    }
}
