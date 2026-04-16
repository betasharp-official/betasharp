using BetaSharp.Client.Guis;

namespace BetaSharp.Client.UI.Rendering;

public interface IUiRenderBackend
{
    void BeginFrame();
    void EndFrame();

    void SetColor(Color color);
    void ResetColor();
    void SetDepthMask(bool enabled);
    void SetAlphaTest(bool enabled);
    void SetBlendFunction(UiBlendFactor source, UiBlendFactor destination);
    void ResetBlendFunction();
    void ClearDepthBuffer();
    void EnableScissor(int x, int y, uint width, uint height);
    void DisableScissor();

    void PushMatrix();
    void PopMatrix();
    void Translate(float x, float y, float z);
    void Scale(float x, float y, float z);
    void Rotate(float angle, float x, float y, float z);

    void SetLighting(bool enabled);
    void SetDepthTest(bool enabled);
    void SetCullFace(bool enabled);
    void SetRescaleNormal(bool enabled);
    void SetColorMaterial(bool enabled);
    void TurnOnLighting(bool mirrored = false);
    void TurnOnGuiLighting();
    void TurnOffLighting();

    void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop, double uRight, double vBottom);
    void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop, double uRight, double vBottom, Color tint);
    void DrawSolidQuad(int x1, int y1, int x2, int y2, Color color);
    void DrawGradientQuad(int left, int top, int right, int bottom, Color topColor, Color bottomColor);
}
