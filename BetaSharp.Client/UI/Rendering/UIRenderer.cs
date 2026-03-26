using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using Silk.NET.Maths;

namespace BetaSharp.Client.UI.Rendering;

public class UIRenderer
{
    private readonly TextRenderer _textRenderer;
    public TextureManager TextureManager { get; }

    private float _translateX = 0;
    private float _translateY = 0;
    private readonly Stack<Vector2D<float>> _translationStack = new();

    public UIRenderer(TextRenderer textRenderer, TextureManager textureManager)
    {
        _textRenderer = textRenderer;
        TextureManager = textureManager;
    }

    public void Begin()
    {
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.PushMatrix();

        _translateX = 0;
        _translateY = 0;
        _translationStack.Clear();
    }

    public void End()
    {
        GLManager.GL.PopMatrix();
    }

    public void PushTranslate(float x, float y)
    {
        _translationStack.Push(new(_translateX, _translateY));
        _translateX += x;
        _translateY += y;
    }

    public void PopTranslate(float x, float y)
    {
        if (_translationStack.Count > 0)
        {
            Vector2D<float> prev = _translationStack.Pop();
            _translateX = prev.X;
            _translateY = prev.Y;
        }
        else
        {
            _translateX = 0;
            _translateY = 0;
        }

        // Stability
        if (MathF.Abs(_translateX) < 0.0001f) _translateX = 0;
        if (MathF.Abs(_translateY) < 0.0001f) _translateY = 0;
    }

    public void EnableClipping(int x, int y, int width, int height)
    {
        BetaSharp game = BetaSharp.Instance;
        ScaledResolution res = new(game.options, game.displayWidth, game.displayHeight);

        int scale = res.ScaleFactor;
        int scaledWindowHeight = game.displayHeight;

        int physicalX = (int)((x + _translateX) * scale);
        int physicalWidth = width * scale;
        int physicalHeight = height * scale;
        int physicalY = scaledWindowHeight - (int)((y + _translateY) * scale) - physicalHeight;

        GLManager.GL.Enable(GLEnum.ScissorTest);
        GLManager.GL.Scissor(physicalX, physicalY, (uint)physicalWidth, (uint)physicalHeight);
    }

    public void DisableClipping()
    {
        GLManager.GL.Disable(GLEnum.ScissorTest);
    }

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        int ix1 = (int)MathF.Floor(x + _translateX);
        int iy1 = (int)MathF.Floor(y + _translateY);
        int ix2 = (int)MathF.Floor(x + _translateX + width);
        int iy2 = (int)MathF.Floor(y + _translateY + height);
        Gui.DrawRect(ix1, iy1, ix2, iy2, color);
    }

    public void DrawGradientRect(float x, float y, float width, float height, Color topColor, Color bottomColor)
    {
        int ix1 = (int)MathF.Floor(x + _translateX);
        int iy1 = (int)MathF.Floor(y + _translateY);
        int ix2 = (int)MathF.Floor(x + _translateX + width);
        int iy2 = (int)MathF.Floor(y + _translateY + height);
        Gui.DrawGradientRect(ix1, iy1, ix2, iy2, topColor, bottomColor);
    }

    public void DrawText(string text, float x, float y, Color color, float scale = 1.0f)
    {
        if (scale == 1.0f)
        {
            _textRenderer.DrawStringWithShadow(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), color);
            return;
        }

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(x + _translateX, y + _translateY, 0);
        GLManager.GL.Scale(scale, scale, 1);
        _textRenderer.DrawStringWithShadow(text, 0, 0, color);
        GLManager.GL.PopMatrix();
    }

    public void DrawCenteredText(string text, float x, float y, Color color, float rotation = 0, float scale = 1.0f)
    {
        if (rotation == 0 && scale == 1.0f)
        {
            Gui.DrawCenteredString(_textRenderer, text, (int)(x + _translateX), (int)(y + _translateY), color);
            return;
        }

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(x + _translateX, y + _translateY, 0);
        if (rotation != 0) GLManager.GL.Rotate(rotation, 0, 0, 1);
        if (scale != 1.0f) GLManager.GL.Scale(scale, scale, 1);

        Gui.DrawCenteredString(_textRenderer, text, 0, 0, color);

        GLManager.GL.PopMatrix();
    }

    public void DrawTexture(TextureHandle texture, float x, float y, float width, float height)
    {
        TextureManager.BindTexture(texture);
        Tessellator tess = Tessellator.instance;
        float finalX = x + _translateX;
        float finalY = y + _translateY;

        tess.startDrawingQuads();
        tess.addVertexWithUV(finalX, finalY + height, 0.0D, 0.0D, 1.0D);
        tess.addVertexWithUV(finalX + width, finalY + height, 0.0D, 1.0D, 1.0D);
        tess.addVertexWithUV(finalX + width, finalY, 0.0D, 1.0D, 0.0D);
        tess.addVertexWithUV(finalX, finalY, 0.0D, 0.0D, 0.0D);
        tess.draw();
    }

    public void DrawTexturedModalRect(TextureHandle texture, float x, float y, float u, float v, float width, float height)
    {
        TextureManager.BindTexture(texture);
        float f = 0.00390625F;
        Tessellator tess = Tessellator.instance;
        float finalX = x + _translateX;
        float finalY = y + _translateY;

        tess.startDrawingQuads();
        tess.addVertexWithUV(finalX + 0, finalY + height, 0.0D, (double)((u + 0) * f), (double)((v + height) * f));
        tess.addVertexWithUV(finalX + width, finalY + height, 0.0D, (double)((u + width) * f), (double)((v + height) * f));
        tess.addVertexWithUV(finalX + width, finalY + 0, 0.0D, (double)((u + width) * f), (double)((v + 0) * f));
        tess.addVertexWithUV(finalX + 0, finalY + 0, 0.0D, (double)((u + 0) * f), (double)((v + 0) * f));
        tess.draw();
    }

    public void DrawRepeatingTexture(TextureHandle texture, float x, float y, float width, float height, float textureScale, float scrollOffsetY = 0f)
    {
        TextureManager.BindTexture(texture);
        Tessellator tess = Tessellator.instance;

        float finalX = x + _translateX;
        float finalY = y + _translateY;

        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x404040);
        tess.addVertexWithUV(finalX, finalY + height, 0.0, finalX / textureScale, (finalY + height + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX + width, finalY + height, 0.0, (finalX + width) / textureScale, (finalY + height + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX + width, finalY, 0.0, (finalX + width) / textureScale, (finalY + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX, finalY, 0.0, finalX / textureScale, (finalY + scrollOffsetY) / textureScale);
        tess.draw();
    }
}
