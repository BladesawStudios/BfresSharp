using BfresSharp.Gx2;

namespace BfresSharp.Model.Geometry;

/// <summary>One attribute of a vertex buffer: its format and where it sits inside one of the buffer's streams.</summary>
public sealed class VertexAttrib
{
    public string Name { get; set; } = "";

    public GX2AttribFormat Format { get; set; }

    public ushort Offset { get; set; }

    public ushort BufferIndex { get; set; }
}
