using BfresSharp.Model;

namespace BfresSharp.Tests;

/// <summary>Checks the dictionary at both sizes, below and above the point where it builds an index.</summary>
public sealed class ResDictTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void LooksUpRenamesAndRemovesAtAnySize(int count)
    {
        var dict = new ResDict<int>();
        for (int index = 0; index < count; index++) dict.Add($"key{index}", index);

        Assert.Equal(count - 1, dict["key" + (count - 1)]);
        Assert.Equal(-1, dict.IndexOf("missing"));
        Assert.Throws<ArgumentException>(() => dict.Add("key0", 99));

        dict.Rename("key1", "renamed");
        Assert.Equal(1, dict["renamed"]);
        Assert.False(dict.ContainsKey("key1"));

        Assert.True(dict.Remove("key0"));
        Assert.Equal(count - 1, dict.Count);
        Assert.Equal(0, dict.IndexOf("renamed"));
        Assert.Equal(count - 1, dict["key" + (count - 1)]);
    }

    [Fact]
    public void KeepsInsertionOrderAcrossTheIndexThreshold()
    {
        var dict = new ResDict<int>();
        for (int index = 0; index < 20; index++) dict.Add($"k{index}", index);

        Assert.Equal(Enumerable.Range(0, 20).Select(index => $"k{index}"), dict.Keys);
        Assert.Equal(7, dict["k7"]);
    }

    [Fact]
    public void RemembersHashesOnlyForTheEntriesThatHaveThem()
    {
        var dict = new ResDict<int>();
        dict.Add("plain", 1);
        dict.Add("hashed", 2, 0xABCDEF);

        Assert.True(dict.HasHashedKeys);
        Assert.Null(dict.HashOf(0));
        Assert.Equal(0xABCDEFul, dict.HashOf(1));
    }
}
