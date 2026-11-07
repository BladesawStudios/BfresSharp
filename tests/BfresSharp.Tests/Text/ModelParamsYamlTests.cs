using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Material;
using BfresSharp.Text;

namespace BfresSharp.Tests.Text;

public sealed class ModelParamsYamlTests
{
    private static ResModel Model()
    {
        var material = new Material
        {
            ShaderParamData = new byte[20],
        };
        material.ShaderParams.Add("ns_param_color1", new ShaderParam { Name = "ns_param_color1", Type = ShaderParamType.Float4, DataOffset = 0 });
        material.ShaderParams.Add("ns_param_flag", new ShaderParam { Name = "ns_param_flag", Type = ShaderParamType.Int, DataOffset = 16 });
        ShaderParamValues.WriteFloats(material, material.ShaderParams["ns_param_color1"], [0.73f, 0.68f, 0.6f, 1f], false);
        ShaderParamValues.WriteInts(material, material.ShaderParams["ns_param_flag"], [3], false);
        var model = new ResModel { Name = "m" };
        model.Materials.Add("Mt_Body", material);
        return model;
    }

    [Fact]
    public void AColourIsOneLineWithItsHex()
    {
        string yaml = ModelParamsYaml.ToYaml(Model());

        Assert.Contains("{name: ns_param_color1, type: Float4, values: [0.73, 0.68, 0.6, 1]}  # #BAAD99", yaml);
        Assert.Contains("{name: ns_param_flag, type: Int, int: 3}", yaml);
    }

    [Fact]
    public void AnEditedColourIsWrittenAndAnUntouchedOneKeepsItsBytes()
    {
        ResModel model = Model();
        byte[] before = model.Materials["Mt_Body"].ShaderParamData.ToArray();

        ModelParamsYaml.Apply(model, ModelParamsYaml.ToYaml(model));
        Assert.Equal(before, model.Materials["Mt_Body"].ShaderParamData);

        string yaml = ModelParamsYaml.ToYaml(model).Replace("values: [0.73, 0.68, 0.6, 1]", "color: \"#FF0000\"");
        ModelParamsYaml.Apply(model, yaml);
        Material material = model.Materials["Mt_Body"];
        Assert.Equal([1f, 0f, 0f, 1f], ShaderParamValues.ReadFloats(material, material.ShaderParams["ns_param_color1"], false));
        Assert.Equal([3], ShaderParamValues.ReadInts(material, material.ShaderParams["ns_param_flag"], false));
    }

    [Fact]
    public void AWrongCountOrNameIsRefused()
    {
        ResModel model = Model();
        string yaml = ModelParamsYaml.ToYaml(model);

        Assert.Contains("takes 4", Assert.Throws<BfresFormatException>(() => ModelParamsYaml.Apply(model, yaml.Replace("[0.73, 0.68, 0.6, 1]", "[1, 2, 3]"))).Message);
        Assert.Throws<BfresFormatException>(() => ModelParamsYaml.Apply(model, yaml.Replace("name: ns_param_flag", "name: nope")));
    }
}
