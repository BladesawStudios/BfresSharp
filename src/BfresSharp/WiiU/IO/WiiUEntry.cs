using BfresSharp.Binary;

namespace BfresSharp.WiiU.IO;

/// <summary>A record the Wii U writer has been told about: where it goes once placed, and the offset fields waiting for that position.</summary>
/// <remarks>The first waiting field is kept inline, because most records are pointed at once.</remarks>
internal sealed class Entry(int index, Action write)
{
    private int _first = -1;
    private List<int>? _more;

    public bool HasFields => _first >= 0;

    // Most records are pointed at once, so the first field is kept inline and only more than one needs a list.
    public void AddField(int field)
    {
        if (_first < 0) _first = field;
        else (_more ??= []).Add(field);
    }

    // Points every field that refers to this entry at its position.
    public void Resolve(ByteBuilder o)
    {
        if (_first < 0) return;
        o.SetU32(_first, (uint)(Target - _first));
        if (_more is null) return;
        foreach (int field in _more) o.SetU32(field, (uint)(Target - field));
    }

    public int Target { get; set; } = -1;

    public int Index => index;

    public Action Write => write;
}
