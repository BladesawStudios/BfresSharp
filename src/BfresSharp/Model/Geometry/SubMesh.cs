namespace BfresSharp.Model.Geometry;

/// <summary>A run of indices of a mesh that is drawn or culled on its own.</summary>
public readonly record struct SubMesh(uint Offset, uint Count);
