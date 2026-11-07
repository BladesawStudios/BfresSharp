namespace BfresSharp.Model.Animation;

/// <summary>A value an animation sets without a curve: the offset of the field in the animated structure and the four bytes to store there.</summary>
public readonly record struct AnimConstant(uint AnimDataOffset, uint Value)
{
    public float AsSingle => BitConverter.UInt32BitsToSingle(Value);

    public int AsInt32 => unchecked((int)Value);
}
