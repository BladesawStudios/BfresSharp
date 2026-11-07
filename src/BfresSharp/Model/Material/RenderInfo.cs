namespace BfresSharp.Model.Material;

/// <summary>The kind of values a render info holds.</summary>
public enum RenderInfoType : byte
{
    Int32,
    Single,
    String,
}

/// <summary>A named render state value of a material: a list of integers, floats or strings.</summary>
/// <remarks>
/// Only the array that matches <see cref="Type"/> is used. Version 10 Switch files may name a render info by hash
/// alone; that hash is kept in <see cref="NameHash"/> and is zero for named entries.
/// </remarks>
public sealed class RenderInfo
{
    public string Name { get; set; } = "";

    public ulong NameHash { get; set; }

    public RenderInfoType Type { get; private set; } = RenderInfoType.Int32;

    public int[] Ints { get; private set; } = [];

    public float[] Floats { get; private set; } = [];

    public string[] Strings { get; private set; } = [];

    public int Count => Type switch
    {
        RenderInfoType.Int32 => Ints.Length,
        RenderInfoType.Single => Floats.Length,
        _ => Strings.Length,
    };

    public void SetValue(int[] values)
    {
        Type = RenderInfoType.Int32;
        Ints = values;
        Floats = [];
        Strings = [];
    }

    public void SetValue(float[] values)
    {
        Type = RenderInfoType.Single;
        Floats = values;
        Ints = [];
        Strings = [];
    }

    public void SetValue(string[] values)
    {
        Type = RenderInfoType.String;
        Strings = values;
        Ints = [];
        Floats = [];
    }
}
