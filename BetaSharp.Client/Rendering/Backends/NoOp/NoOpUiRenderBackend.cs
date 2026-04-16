using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpUiRenderBackend : IUiRenderBackend
{
    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
    }

    public void SetColor(Color color)
    {
    }

    public void ResetColor()
    {
    }

    public void SetDepthMask(bool enabled)
    {
    }

    public void SetAlphaTest(bool enabled)
    {
    }

    public void SetBlendFunction(UiBlendFactor source, UiBlendFactor destination)
    {
    }

    public void ResetBlendFunction()
    {
    }

    public void ClearDepthBuffer()
    {
    }

    public void EnableScissor(int x, int y, uint width, uint height)
    {
    }

    public void DisableScissor()
    {
    }

    public void PushMatrix()
    {
    }

    public void PopMatrix()
    {
    }

    public void Translate(float x, float y, float z)
    {
    }

    public void Scale(float x, float y, float z)
    {
    }

    public void Rotate(float angle, float x, float y, float z)
    {
    }

    public void SetLighting(bool enabled)
    {
    }

    public void SetDepthTest(bool enabled)
    {
    }

    public void SetCullFace(bool enabled)
    {
    }

    public void SetRescaleNormal(bool enabled)
    {
    }

    public void SetColorMaterial(bool enabled)
    {
    }

    public void TurnOnLighting(bool mirrored = false)
    {
    }

    public void TurnOnGuiLighting()
    {
    }

    public void TurnOffLighting()
    {
    }

    public void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop, double uRight, double vBottom)
    {
    }

    public void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop, double uRight, double vBottom, Color tint)
    {
    }

    public void DrawSolidQuad(int x1, int y1, int x2, int y2, Color color)
    {
    }

    public void DrawGradientQuad(int left, int top, int right, int bottom, Color topColor, Color bottomColor)
    {
    }
}
