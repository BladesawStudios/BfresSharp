using System.Numerics;
using BfresSharp.Model.Material;
using BfresSharp.WiiU.IO;

namespace BfresSharp.WiiU.Models;

/// <summary>Reads and writes render states in the Wii U layout, which is 48 bytes.</summary>
/// <remarks>
/// The flags, the polygon, depth and alpha control registers, the alpha reference, the color control register, the blend
/// target, the blend control register and the blend color.
/// </remarks>
public static class RenderStateCodec
{
    public static RenderState Read(WiiUReader reader, long at)
    {
        var view = reader.View;
        return new RenderState
        {
            Flags = view.U32(at),
            PolygonControl = new PolygonControl(view.U32(at + 4)),
            DepthControl = new DepthControl(view.U32(at + 8)),
            AlphaControl = new AlphaControl(view.U32(at + 12)),
            AlphaRefValue = view.F32(at + 16),
            ColorControl = new ColorControl(view.U32(at + 20)),
            BlendTarget = view.U32(at + 24),
            BlendControl = new BlendControl(view.U32(at + 28)),
            BlendColor = new Vector4(view.F32(at + 32), view.F32(at + 36), view.F32(at + 40), view.F32(at + 44)),
        };
    }

    public static void Write(WiiUWriter writer, RenderState state)
    {
        writer.Out.U32(state.Flags).U32(state.PolygonControl.Value).U32(state.DepthControl.Value).U32(state.AlphaControl.Value)
            .F32(state.AlphaRefValue).U32(state.ColorControl.Value).U32(state.BlendTarget).U32(state.BlendControl.Value)
            .F32(state.BlendColor.X).F32(state.BlendColor.Y).F32(state.BlendColor.Z).F32(state.BlendColor.W);
    }
}
