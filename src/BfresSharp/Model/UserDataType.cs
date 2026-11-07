namespace BfresSharp.Model;

/// <summary>The kind of values a <see cref="UserData"/> entry holds.</summary>
/// <remarks>The values are the type byte stored in the file.</remarks>
public enum UserDataType : byte
{
    Int32,
    Single,
    String,
    WString,
    Byte,
}
