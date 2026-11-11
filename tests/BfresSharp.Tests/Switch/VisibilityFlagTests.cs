using BfresSharp.Container;
using BfresSharp.Gx2;
using BfresSharp.Model;
using BfresSharp.Model.Geometry;
using BfresSharp.Model.Skeleton;

namespace BfresSharp.Tests.Switch;

/// <summary>The visible bit of a material and of a bone is written, read back and left alone when a file is written a second time.</summary>
public sealed class VisibilityFlagTests
{
    public static TheoryData<BfresPlatform, uint> Versions => new()
    {
        { BfresPlatform.Switch, 0x00050003 },
        { BfresPlatform.Switch, 0x00090000 },
        { BfresPlatform.Switch2, 0x000A0400 },
    };

    private static ResFile Build(BfresPlatform platform, uint raw, bool materialVisible, bool boneVisible)
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
        var model = new ResModel { Name = "quad" };
        model.Skeleton.Bones.Add("root", new Bone { Name = "root", Visible = boneVisible });
        var buffer = new VertexBuffer { VertexCount = 3 };
        buffer.Attributes.Add("_p0", new VertexAttrib { Name = "_p0", Format = GX2AttribFormat.Format_32_32_32_Single, Offset = 0, BufferIndex = 0 });
        buffer.Streams.Add(new VertexStream { Stride = 12, Data = new byte[36] });
        model.VertexBuffers.Add(buffer);
        var mesh = new Mesh { PrimitiveType = GX2PrimitiveType.Triangles, IndexFormat = IndexFormat.UInt16, IndexData = new byte[] { 0, 0, 1, 0, 2, 0 } };
        model.Shapes.Add("shape", new Shape { Name = "shape", Meshes = [mesh] });
        model.Materials.Add("mat", new BfresSharp.Model.Material.Material { Name = "mat", Visible = materialVisible });
        file.Models.Add("quad", model);
        return file;
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void AnInvisibleMaterialAndBoneStayInvisibleThroughAWriteAndARead(BfresPlatform platform, uint raw)
    {
        ResFile read = BfresFile.Read(BfresFile.Write(Build(platform, raw, false, false)));
        ResModel model = read.Models["quad"];
        Assert.False(model.Materials["mat"].Visible);
        Assert.False(model.Skeleton.Bones["root"].Visible);
    }

    [Theory]
    [MemberData(nameof(Versions))]
    public void VisibleStaysVisibleAndOnlyTheLowBitChanges(BfresPlatform platform, uint raw)
    {
        ResFile file = Build(platform, raw, true, true);
        file.Models["quad"].Materials["mat"].Flags = 0x30001;
        ResFile read = BfresFile.Read(BfresFile.Write(file));
        Assert.True(read.Models["quad"].Materials["mat"].Visible);
        read.Models["quad"].Materials["mat"].Visible = false;
        ResFile again = BfresFile.Read(BfresFile.Write(read));
        Assert.Equal(0x30000u, again.Models["quad"].Materials["mat"].Flags);
    }
}
