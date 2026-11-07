namespace BfresSharp.Model;

/// <summary>A named array of values that a tool attached to a section of the file.</summary>
/// <remarks>
/// Holds exactly one array, of the type <see cref="Type"/> says: ints, floats, strings (UTF-8 or UTF-16) or bytes. The
/// bytes of the 43-byte reserved area of the Switch layout are kept so an entry writes back as it was read.
/// </remarks>
public sealed class UserData
{
    public string Name { get; set; } = "";

    public UserDataType Type { get; private set; }

    public Array Value { get; private set; } = Array.Empty<int>();

    public byte[] Reserved { get; set; } = new byte[43];

    public void SetInts(int[] values) => Set(UserDataType.Int32, values);

    public void SetFloats(float[] values) => Set(UserDataType.Single, values);

    public void SetStrings(string[] values, bool wide = false) => Set(wide ? UserDataType.WString : UserDataType.String, values);

    public void SetBytes(sbyte[] values) => Set(UserDataType.Byte, values);

    public int[] GetInts() => (int[])Value;

    public float[] GetFloats() => (float[])Value;

    public string[] GetStrings() => (string[])Value;

    public sbyte[] GetBytes() => (sbyte[])Value;

    private void Set(UserDataType type, Array value)
    {
        Type = type;
        Value = value;
    }
}
