using System.Text;
using BfresSharp.Errors;
using BfresSharp.Model;
using BfresSharp.Switch.IO;

namespace BfresSharp.Switch.Common;

/// <summary>Reads and writes the user data entries of a Switch file.</summary>
/// <remarks>
/// An entry is 64 bytes: the name pointer, a pointer to the values, the count, the type byte and 43 reserved bytes.
/// The values are stored separately: numbers as plain arrays, strings as an array of pointers to the name pool.
/// Entries are written first, then the dictionary, then the values grouped by type: strings, wide strings, ints,
/// floats and bytes.
/// </remarks>
public static class UserDataCodec
{
    public const int EntrySize = 64;

    public static UserData Read(SwitchReader reader, long at)
    {
        var data = new UserData { Name = reader.String(at) ?? "" };
        long values = reader.Pointer(at + 8);
        int count = reader.View.I32(at + 16);
        var type = (UserDataType)reader.View.U8(at + 20);
        data.Reserved = reader.View.Slice(at + 21, 43).Span.ToArray();
        switch (type)
        {
            case UserDataType.Int32: data.SetInts(ReadAll(count, i => reader.View.I32(values + i * 4L))); break;
            case UserDataType.Single: data.SetFloats(ReadAll(count, i => reader.View.F32(values + i * 4L))); break;
            case UserDataType.Byte: data.SetBytes(ReadAll(count, i => (sbyte)reader.View.U8(values + i))); break;
            case UserDataType.String: data.SetStrings(ReadStrings(reader, values, count)); break;
            case UserDataType.WString: data.SetStrings(ReadStrings(reader, values, count), wide: true); break;
            default: throw new BfresFormatException($"Unknown user data type {(byte)type}", at + 20);
        }

        return data;
    }

    public static ResDict<UserData> ReadDict(SwitchReader reader, long dictPointerAt, long arrayPointerAt)
    {
        return reader.ReadDict(dictPointerAt, arrayPointerAt, EntrySize, at => Read(reader, at));
    }

    // Writes the entries of a section's user data and returns the slots their values pointers must be resolved to. Files
    // before version 9 point the array at this spot even when it holds nothing.
    public static int[] WriteArray(SwitchWriter writer, ResDict<UserData> userData, int arraySlot)
    {
        if (userData.Count == 0 && writer.Profile.Modern) return [];
        writer.Align(8).Resolve(arraySlot);
        return WriteEntries(writer, userData);
    }

    // Writes the dictionary and then the values, for sections that keep both at the end.
    public static void WriteDictAndValues(SwitchWriter writer, ResDict<UserData> userData, int dictSlot, int[] valueSlots)
    {
        WriteDict(writer, userData, dictSlot);
        WriteValuesAligned(writer, userData, valueSlots);
    }

    public static void WriteDict(SwitchWriter writer, ResDict<UserData> userData, int dictSlot)
    {
        if (userData.Count == 0) return;
        writer.Align(8).Resolve(dictSlot);
        SwitchDictCodec.Write(writer, userData);
    }

    public static void WriteValuesAligned(SwitchWriter writer, ResDict<UserData> userData, int[] valueSlots)
    {
        if (userData.Count == 0) return;
        WriteValues(writer, userData, valueSlots);
        writer.Align(8);
    }

    // Writes the entries and returns, per entry, the slot its values pointer must be resolved to.
    public static int[] WriteEntries(SwitchWriter writer, ResDict<UserData> userData)
    {
        var slots = new int[userData.Count];
        for (int index = 0; index < userData.Count; index++)
        {
            UserData data = userData.Values[index];
            writer.StringPointer(data.Name);
            slots[index] = data.Value.Length == 0 ? writer.Unlisted() : writer.Pointer();
            writer.Out.U32((uint)data.Value.Length).U8((byte)data.Type).Bytes(data.Reserved);
        }

        return slots;
    }

    public static void WriteValues(SwitchWriter writer, ResDict<UserData> userData, int[] slots)
    {
        foreach (UserDataType type in new[] { UserDataType.String, UserDataType.WString, UserDataType.Int32, UserDataType.Single, UserDataType.Byte })
        {
            for (int index = 0; index < userData.Count; index++)
            {
                UserData data = userData.Values[index];
                if (data.Type != type || data.Value.Length == 0) continue;
                writer.Align(8).Resolve(slots[index]);
                WriteValue(writer, data);
            }
        }
    }

    private static void WriteValue(SwitchWriter writer, UserData data)
    {
        switch (data.Type)
        {
            case UserDataType.Int32: foreach (int value in data.GetInts()) writer.Out.U32((uint)value); break;
            case UserDataType.Single: foreach (float value in data.GetFloats()) writer.Out.F32(value); break;
            case UserDataType.Byte: foreach (sbyte value in data.GetBytes()) writer.Out.U8((byte)value); break;
            default: foreach (string value in data.GetStrings()) writer.StringPointer(value); break;
        }
    }

    private static string[] ReadStrings(SwitchReader reader, long values, int count)
    {
        return ReadAll(count, i => reader.String(values + i * 8L) ?? "");
    }

    private static T[] ReadAll<T>(int count, Func<int, T> read)
    {
        var result = new T[count];
        for (int index = 0; index < count; index++) result[index] = read(index);
        return result;
    }
}
