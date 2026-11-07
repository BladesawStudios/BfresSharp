using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Model.Animation;
using BfresSharp.Text;
using BfresSharp.Tests.Corpus;
using Xunit.Abstractions;

namespace BfresSharp.Tests.Text;

public sealed class MaterialAnimYamlTests(ITestOutputHelper output)
{
    private static MaterialAnim Sample()
    {
        var item = new MaterialAnimItem
        {
            Name = "Mt_Body",
            ShaderParamCurveIndex = 0,
            TexturePatternCurveIndex = 1,
            VisualConstantIndex = 1,
            VisualCurveIndex = 2,
            BeginVisualConstantIndex = 0,
            ParamAnimInfos = [new ParamAnimInfo { Name = "color", BeginCurve = 0, FloatCurveCount = 1, BeginConstant = 0, ConstantCount = 1, SubBindIndex = 4 }],
            PatternAnimInfos = [new PatternAnimInfo { Name = "_a0", CurveIndex = 1 }],
            Curves =
            [
                new AnimCurve
                {
                    FrameType = AnimCurveFrameType.Single,
                    KeyType = AnimCurveKeyType.Single,
                    CurveType = AnimCurveType.Linear,
                    PostWrap = WrapMode.Repeat,
                    EndFrame = 2.5f,
                    Scale = 0.1f,
                    Frames = [0f, 1.25f, 2.5f],
                    Keys = [0f, 1f, 1f, -0.5f, 1f, 3f],
                },
                new AnimCurve
                {
                    FrameType = AnimCurveFrameType.Byte,
                    CurveType = AnimCurveType.StepBool,
                    Frames = [0, 4],
                    BoolKeys = [true, false],
                },
                new AnimCurve
                {
                    Flags = AnimCurve.CompressedFlag,
                    KeyType = AnimCurveKeyType.SByte,
                    CurveType = AnimCurveType.StepInt,
                    FrameType = AnimCurveFrameType.Byte,
                    CompressedHeaderWord = 7,
                    Frames = [0, 1],
                    CompressedKeyData = [0x00, 0x80],
                },
            ],
            Constants = [new AnimConstant(16, BitConverter.SingleToUInt32Bits(0.75f)), new AnimConstant(20, 0xDEADBEEF)],
        };
        var anim = new MaterialAnim
        {
            Name = "Status_Red",
            Path = "a/b",
            Flags = 4,
            FrameCount = 12,
            BindIndices = [0, ushort.MaxValue],
            Items = [item],
            TextureNames = ["Body_Alb.00", "Body_Alb.01"],
            TextureBinds = [-1, 5000000000],
        };
        var user = new UserData { Name = "note" };
        user.SetFloats([1.5f, -2f]);
        anim.UserData.Add("note", user);
        var text = new UserData { Name = "label" };
        text.SetStrings(["a", "b: c"]);
        anim.UserData.Add("label", text);
        return anim;
    }

    [Fact]
    public void ASectionSurvivesATripThroughYaml()
    {
        MaterialAnim original = Sample();

        MaterialAnim back = MaterialAnimYaml.FromRawYaml(MaterialAnimYaml.ToRawYaml(original));

        Assert.Equal(MaterialAnimYaml.ToRawYaml(original), MaterialAnimYaml.ToRawYaml(back));
        Assert.Equal(original.Items[0].Curves[0].Flags, back.Items[0].Curves[0].Flags);
        Assert.Equal(original.Items[0].Curves[2].Flags, back.Items[0].Curves[2].Flags);
        Assert.Equal(original.Items[0].Curves[0].Keys, back.Items[0].Curves[0].Keys);
        Assert.Equal(original.Items[0].Curves[1].BoolKeys, back.Items[0].Curves[1].BoolKeys);
        Assert.Equal(original.Items[0].Curves[2].CompressedKeyData, back.Items[0].Curves[2].CompressedKeyData);
                Assert.Equal(original.TextureBinds, back.TextureBinds);
        Assert.Equal(original.UserData["note"].GetFloats(), back.UserData["note"].GetFloats());
        Assert.Equal(original.UserData["label"].GetStrings(), back.UserData["label"].GetStrings());
    }

    [Fact]
    public void EditingTheFloatOfAConstantChangesItsWord()
    {
        string yaml = MaterialAnimYaml.ToRawYaml(Sample()).Replace("Float: 0.75", "Float: 2");

        MaterialAnim edited = MaterialAnimYaml.FromRawYaml(yaml, Sample());

        Assert.Equal(2f, edited.Items[0].Constants[0].AsSingle);
        Assert.Equal(0xDEADBEEFu, edited.Items[0].Constants[1].Value);
    }

    [Fact]
    public void EditingTheRawWordOfAConstantChangesIt()
    {
        string yaml = MaterialAnimYaml.ToRawYaml(Sample()).Replace("Value: 3735928559", "Value: 7");

        Assert.Equal(7u, MaterialAnimYaml.FromRawYaml(yaml, Sample()).Items[0].Constants[1].Value);
    }

    [Fact]
    public void ACurveWithBadWrapIsRejected()
    {
        string yaml = MaterialAnimYaml.ToRawYaml(Sample()).Replace("PostWrap: Repeat", "PostWrap: Spin");

        var error = Assert.Throws<BfresFormatException>(() => MaterialAnimYaml.FromRawYaml(yaml));

        Assert.Contains("PostWrap", error.Message);
    }

    [Fact]
    public void TheUnnamedFourthWrapModeKeepsItsNumber()
    {
        MaterialAnim anim = Sample();
        anim.Items[0].Curves[0].PostWrap = (WrapMode)3;
        string yaml = MaterialAnimYaml.ToRawYaml(anim);

        Assert.Contains("PostWrap: 3", yaml);
        Assert.Equal(anim.Items[0].Curves[0].Flags, MaterialAnimYaml.FromRawYaml(yaml).Items[0].Curves[0].Flags);
        Assert.Throws<BfresFormatException>(() => MaterialAnimYaml.FromRawYaml(yaml.Replace("PostWrap: 3", "PostWrap: 4")));
    }

    [Fact]
    public void AnUnknownFieldIsRejectedWithItsLine()
    {
        var error = Assert.Throws<BfresFormatException>(() => MaterialAnimYaml.FromYaml("name: x\nframeCuont: 3\n"));

        Assert.Contains("Line 2", error.Message);
    }

    [Fact]
    public void EmptyYamlIsRejected()
    {
        Assert.Throws<BfresFormatException>(() => MaterialAnimYaml.FromYaml(""));
    }

    [CorpusFact]
    public void EveryMaterialAnimationOfTheCorpusWritesBackIdentically()
    {
        int checkedCount = 0;
        var failures = new List<string>();
        foreach (string folder in GameFiles.Folders)
        {
            foreach ((string name, byte[] data) in GameFiles.BfresFiles(folder))
            {
                ResFile file;
                try
                {
                    file = BfresFile.Read(data);
                }
                catch (BfresFormatException)
                {
                    continue;
                }

                if (file.MaterialAnims.Count == 0) continue;
                byte[] before = BfresFile.Write(file);
                foreach (string key in file.MaterialAnims.Keys.ToList())
                {
                    file.MaterialAnims[key] = MaterialAnimYaml.FromYaml(MaterialAnimYaml.ToYaml(file.MaterialAnims[key]));
                    checkedCount++;
                }

                if (!BfresFile.Write(file).AsSpan().SequenceEqual(before)) failures.Add(name);
            }
        }

        output.WriteLine($"checked {checkedCount} material animations");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(10)));
    }

    [Fact]
    public void TheReadableFormNestsCurvesUnderTheirParameters()
    {
        string yaml = MaterialAnimYaml.ToYaml(Sample());

        Assert.Contains("- material: Mt_Body", yaml);
        Assert.Contains("name: color", yaml);
        Assert.Contains("{frame: 1.25, value: 1, delta: -0.5}", yaml);
        Assert.DoesNotContain("BeginCurve", yaml);
        Assert.Equal(MaterialAnimYaml.ToRawYaml(Sample()), MaterialAnimYaml.ToRawYaml(MaterialAnimYaml.FromYaml(yaml)));
    }

    [Fact]
    public void EditingAKeyValueInTheReadableFormChangesTheCurve()
    {
        string yaml = MaterialAnimYaml.ToYaml(Sample()).Replace("{frame: 1.25, value: 1, delta: -0.5}", "{frame: 2, value: 7, delta: -0.5}");

        MaterialAnim edited = MaterialAnimYaml.FromYaml(yaml);

        Assert.Equal([0f, 2f, 2.5f], edited.Items[0].Curves[0].Frames);
        Assert.Equal(7f, edited.Items[0].Curves[0].Keys[2]);
    }

    [Fact]
    public void AConstantIsEditedByItsFloat()
    {
        string yaml = MaterialAnimYaml.ToYaml(Sample()).Replace("value: 0.75", "value: 2");

        Assert.Equal(2f, MaterialAnimYaml.FromYaml(yaml).Items[0].Constants[0].AsSingle);
    }

    [Fact]
    public void AnEightBitCurveKeepsItsStepsUntilAValueChanges()
    {
        var anim = new MaterialAnim { Name = "n", TextureNames = ["a", "b", "c"], Items = [new MaterialAnimItem
        {
            Name = "m",
            TexturePatternCurveIndex = 0,
            PatternAnimInfos = [new PatternAnimInfo { Name = "_a0", CurveIndex = 0 }],
            Curves = [new AnimCurve { Flags = AnimCurve.CompressedFlag, KeyType = AnimCurveKeyType.SByte, CurveType = AnimCurveType.StepInt, FrameType = AnimCurveFrameType.Byte, Scale = 1, Offset = 1, Frames = [0, 5], CompressedKeyData = [0, 1] }],
        }] };
        string yaml = MaterialAnimYaml.ToYaml(anim);

        Assert.Contains("{frame: 0, texture: b}", yaml);
        Assert.Equal([0, 1], MaterialAnimYaml.FromYaml(yaml).Items[0].Curves[0].CompressedKeyData);
        MaterialAnim edited = MaterialAnimYaml.FromYaml(yaml.Replace("{frame: 5, texture: c}", "{frame: 5, texture: a}"));
        Assert.Contains("{frame: 5, texture: a}", MaterialAnimYaml.ToYaml(edited));
        Assert.Contains("{frame: 0, texture: b}", MaterialAnimYaml.ToYaml(edited));
    }

    [Fact]
    public void AHashedParameterShowsItsNameAndIsStoredAsTheHashAgain()
    {
        const ulong hash = 0x68E131E72BCD91AD;
        var anim = new MaterialAnim { Name = "n", Items = [new MaterialAnimItem
        {
            Name = "m",
            ShaderParamCurveIndex = ushort.MaxValue,
            TexturePatternCurveIndex = ushort.MaxValue,
            VisualConstantIndex = ushort.MaxValue,
            VisualCurveIndex = ushort.MaxValue,
            BeginVisualConstantIndex = ushort.MaxValue,
            ParamAnimInfos = [new ParamAnimInfo { Name = $"0x{hash:X16}", NameHash = hash, ConstantCount = 1, SubBindIndex = ushort.MaxValue }],
            Constants = [new AnimConstant(0, BitConverter.SingleToUInt32Bits(1f))],
        }] };
        var names = new Dictionary<ulong, string> { [hash] = "ns_param_color0" };

        string yaml = MaterialAnimYaml.ToYaml(anim, names);
        ParamAnimInfo back = MaterialAnimYaml.FromYaml(yaml, null, names).Items[0].ParamAnimInfos[0];

        Assert.Contains("name: ns_param_color0", yaml);
        Assert.Equal(hash, back.NameHash);
        Assert.Equal($"0x{hash:X16}", back.Name);
        Assert.Contains("name: 0x68E131E72BCD91AD", MaterialAnimYaml.ToYaml(anim));
    }

    [Fact]
    public void AFixedColourIsOneLineWithItsHexAndCanBeTypedAsHex()
    {
        var item = new MaterialAnimItem
        {
            Name = "m",
            ShaderParamCurveIndex = ushort.MaxValue,
            TexturePatternCurveIndex = ushort.MaxValue,
            VisualConstantIndex = ushort.MaxValue,
            VisualCurveIndex = ushort.MaxValue,
            BeginVisualConstantIndex = ushort.MaxValue,
            ParamAnimInfos = [new ParamAnimInfo { Name = "ns_param_color0", ConstantCount = 4, SubBindIndex = ushort.MaxValue }],
            Constants = [.. new[] { 1f, 0f, 1f, 1f }.Select((value, index) => new AnimConstant((uint)index * 4, BitConverter.SingleToUInt32Bits(value)))],
        };
        var anim = new MaterialAnim { Name = "n", Items = [item] };

        string yaml = MaterialAnimYaml.ToYaml(anim);

        Assert.Contains("{name: ns_param_color0, values: [1, 0, 1, 1]}  # #FF00FF", yaml);
        string typed = yaml.Replace("values: [1, 0, 1, 1]", "color: \"#00FF80\"");
        AnimConstant[] back = [.. MaterialAnimYaml.FromYaml(typed).Items[0].Constants];
        Assert.Equal([0f, 1f, 128 / 255f, 1f], back.Select(constant => constant.AsSingle));
        Assert.Equal(12u, back[3].AnimDataOffset);
    }
}
