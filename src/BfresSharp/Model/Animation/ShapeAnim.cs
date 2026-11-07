namespace BfresSharp.Model.Animation;

/// <summary>An FSHA section: the animation of vertex shapes, which blend key shapes of a shape by weight.</summary>
/// <remarks>
/// Each <see cref="VertexShapeAnim"/> animates one shape; <see cref="BindIndices"/> says which shape of the bound model
/// each one drives. The header's counts of curves and key shapes are the sums over the vertex shape animations.
/// </remarks>
public sealed class ShapeAnim
{
    private const uint BakedMask = 1 << 0;
    private const uint LoopMask = 1 << 2;

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public uint Flags { get; set; }

    public int FrameCount { get; set; }

    public uint BakedSize { get; set; }

    public bool Loop
    {
        get => (Flags & LoopMask) != 0;
        set => Flags = value ? Flags | LoopMask : Flags & ~LoopMask;
    }

    public bool Baked
    {
        get => (Flags & BakedMask) != 0;
        set => Flags = value ? Flags | BakedMask : Flags & ~BakedMask;
    }

    public ushort[] BindIndices { get; set; } = [];

    public List<VertexShapeAnim> VertexShapeAnims { get; set; } = [];

    public ResDict<UserData> UserData { get; set; } = new();
}
