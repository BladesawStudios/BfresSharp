using BfresSharp.Model;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Models;

/// <summary>Writes the materials of one model in the layout its version uses.</summary>
/// <remarks>
/// Version 10 models also store the shader assignment records their materials share, between the material headers and
/// the shape headers; earlier models have no such records.
/// </remarks>
public sealed class MaterialSection
{
    private readonly bool _v10;
    private readonly ResModel _model;
    private readonly List<MaterialCodec.Slots> _legacy = [];
    private readonly List<MaterialV10Writer.Slots> _slots = [];

    public MaterialSection(SwitchProfile profile, ResModel model)
    {
        _v10 = profile.Version.Major >= 10;
        _model = model;
        Assigns = new ShaderAssignSet(_v10 ? model.Materials.Values : []);
    }

    public ShaderAssignSet Assigns { get; }

    public void WriteHeaders(SwitchWriter writer)
    {
        for (int index = 0; index < _model.Materials.Count; index++)
        {
            if (_v10) _slots.Add(MaterialV10Writer.WriteHeader(writer, _model.Materials.Values[index], index));
            else _legacy.Add(MaterialCodec.WriteHeader(writer, _model.Materials.Values[index], index));
        }
    }

    public void WriteAssignRecords(SwitchWriter writer, int arraySlot)
    {
        if (Assigns.Count == 0) return;
        writer.Align(8).Resolve(arraySlot);
        Assigns.WriteRecords(writer);
    }

    public void WriteBodies(SwitchWriter writer)
    {
        for (int index = 0; index < _model.Materials.Count; index++)
        {
            if (_v10) MaterialV10Writer.WriteBody(writer, _model.Materials.Values[index], _slots[index], Assigns);
            else MaterialCodec.WriteBody(writer, _model.Materials.Values[index], _legacy[index]);
        }

        if (!_v10) return;
        // The shared assignments follow every material.
        foreach (var material in _model.Materials.Values) Assigns.WriteBodyOnce(writer, material);
    }
}
