using BfresSharp.Container;
using BfresSharp.Gx2;
using BfresSharp.Model;
using BfresSharp.Model.Geometry;
using BfresSharp.Model.Skeleton;

namespace BfresSharp.Tests.Switch;

/// <summary>Renaming, removing and putting back the entries of a model's dictionaries leaves a file that writes and reads back as edited.</summary>
public sealed class DictionaryEditTests
{
    public static TheoryData<BfresPlatform, uint> Versions => new()
    {
        { BfresPlatform.Switch, 0x00050003 },
        { BfresPlatform.Switch, 0x00090000 },
        { BfresPlatform.Switch2, 0x000A0400 },
    };

    private static ResFile Build(BfresPlatform platform, uint raw)
    {
        var file = new ResFile
        {
            Name = "synthetic",
            Platform = platform,
            Version = new BfresVersion(platform, raw),
            DataAlignment = 0x2000,
            Alignment = 0x0C,
            TargetAddressSize = 0x40,
        };
        var model = new ResModel { Name = "m" };
        model.Skeleton.Bones.Add("root", new Bone { Name = "root" });
        var buffer = new VertexBuffer { VertexCount = 3 };
        buffer.Attributes.Add("_p0", new VertexAttrib { Name = "_p0", Format = GX2AttribFormat.Format_32_32_32_Single, Offset = 0, BufferIndex = 0 });
        buffer.Streams.Add(new VertexStream { Stride = 12, Data = new byte[36] });
        model.VertexBuffers.Add(buffer);
        foreach (string name in new[] { "a", "b", "c" })
        {
            var mesh = new Mesh { PrimitiveType = GX2PrimitiveType.Triangles, IndexFormat = IndexFormat.UInt16, IndexData = new byte[] { 0, 0, 1, 0, 2, 0 } };
            model.Shapes.Add(name, new Shape { Name = name, Meshes = [mesh] });
        }

        model.Materials.Add("mat", new BfresSharp.Model.Material.Material { Name = "mat" });
        file.Models.Add("m", model);
        return file;
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void ARenamedAndARemovedShapeAreWrittenAsEdited(BfresPlatform platform, uint raw)
    {
        ResFile file = Build(platform, raw);
        ResModel model = file.Models["m"];
        model.Shapes.Rename("a", "first");
        model.Shapes["first"].Name = "first";
        model.Shapes.Remove("b");
        ResModel read = BfresFile.Read(BfresFile.Write(file)).Models["m"];
        Assert.Equal(["first", "c"], read.Shapes.Keys);
        Assert.Equal("first", read.Shapes["first"].Name);
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void ARemovedShapePutBackWithInsertIsWhereItWas(BfresPlatform platform, uint raw)
    {
        ResFile file = Build(platform, raw);
        ResModel model = file.Models["m"];
        Shape taken = model.Shapes["b"];
        int at = model.Shapes.IndexOf("b");
        model.Shapes.RemoveAt(at);
        model.Shapes.Insert(at, "b", taken);
        ResModel read = BfresFile.Read(BfresFile.Write(file)).Models["m"];
        Assert.Equal(["a", "b", "c"], read.Shapes.Keys);
    }

    [Fact]
    public void InsertKeepsTheHashOfAnEntryAndRefusesADuplicate()
    {
        var dict = new ResDict<int>();
        dict.Add("x", 1, 0xABCUL);
        dict.Add("y", 2);
        int at = dict.IndexOf("x");
        ulong? hash = dict.HashOf(at);
        dict.RemoveAt(at);
        dict.Insert(0, "x", 1, hash);
        Assert.Equal(0xABCUL, dict.HashOf(0));
        Assert.Null(dict.HashOf(1));
        Assert.Throws<ArgumentException>(() => dict.Insert(0, "y", 3));
    }
}
