using System.Numerics;

namespace BfresSharp.Model.Geometry;

/// <summary>An axis-aligned box given by its center and half extent.</summary>
public readonly record struct Bounding(Vector3 Center, Vector3 Extent);
