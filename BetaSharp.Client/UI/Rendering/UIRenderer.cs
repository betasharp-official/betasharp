using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;

namespace BetaSharp.Client.UI.Rendering;

public class UIRenderer
{
    private readonly TextRenderer _textRenderer;
    private readonly TextureManager _textureManager;

    private float _translateX = 0;
    private float _translateY = 0;

    public UIRenderer(TextRenderer textRenderer, TextureManager textureManager)
    {
        _textRenderer = textRenderer;
        _textureManager = textureManager;
    }

    public void Begin()
    {
        GLManager.GL.PushMatrix();

        _translateX = 0;
        _translateY = 0;
    }

    public static void End() => GLManager.GL.PopMatrix();

    public void PushTranslate(float x, float y)
    {
        _translateX += x;
        _translateY += y;
    }

    public void PopTranslate(float x, float y)
    {
        _translateX -= x;
        _translateY -= y;
    }

    public void EnableClipping(int x, int y, int width, int height)
    {
        var game = BetaSharp.Instance;
        ScaledResolution res = new ScaledResolution(game.options, game.displayWidth, game.displayHeight);
        
        int scale = res.ScaleFactor;
        int scaledWindowHeight = game.displayHeight;
        
        int physicalX = (int)((x + _translateX) * scale);
        int physicalWidth = (int)(width * scale);
        int physicalHeight = (int)(height * scale);
        int physicalY = scaledWindowHeight - (int)((y + _translateY) * scale) - physicalHeight;

        GLManager.GL.Enable((global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum)0x0C11);
        GLManager.GL.Scissor(physicalX, physicalY, (uint)physicalWidth, (uint)physicalHeight);
    }
    
    public void DisableClipping()
    {
        GLManager.GL.Disable((global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum)0x0C11);
    }

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        int ix1 = (int)(x + _translateX);
        int iy1 = (int)(y + _translateY);
        int ix2 = ix1 + (int)width;
        int iy2 = iy1 + (int)height;
        Gui.DrawRect(ix1, iy1, ix2, iy2, color);
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        _textRenderer.DrawStringWithShadow(text, (int)(x + _translateX), (int)(y + _translateY), color);
    }

    public void DrawCenteredText(string text, float x, float y, Color color)
    {
        Gui.DrawCenteredString(_textRenderer, text, (int)(x + _translateX), (int)(y + _translateY), color);
    }

    public void DrawTexture(TextureHandle texture, float x, float y, float width, float height)
    {
        _textureManager.BindTexture(texture);
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
        _textureManager.BindTexture(texture);
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
        _textureManager.BindTexture(texture);
        Tessellator tess = Tessellator.instance;

        float finalX = x + _translateX;
        float finalY = y + _translateY;
        
        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x202020);
        tess.addVertexWithUV(finalX, finalY + height, 0.0, finalX / textureScale, (finalY + height + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX + width, finalY + height, 0.0, (finalX + width) / textureScale, (finalY + height + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX + width, finalY, 0.0, (finalX + width) / textureScale, (finalY + scrollOffsetY) / textureScale);
        tess.addVertexWithUV(finalX, finalY, 0.0, finalX / textureScale, (finalY + scrollOffsetY) / textureScale);
        tess.draw();
    }

    public void DrawGradientRect(float x, float y, float width, float height, Color topColor, Color bottomColor)
    {
        float finalX = x + _translateX;
        float finalY = y + _translateY;

        var gl = GLManager.GL;
        var glEnum = typeof(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum);

        gl.Disable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Texture2D);
        gl.Enable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Blend);
        gl.Disable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.AlphaTest);
        gl.BlendFunc(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.SrcAlpha, global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.OneMinusSrcAlpha);
        gl.ShadeModel(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Smooth);

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();

        // Bottom-Right
        tess.setColorRGBA(bottomColor);
        tess.addVertex(finalX + width, finalY + height, 0.0);
        // Bottom-Left
        tess.setColorRGBA(bottomColor);
        tess.addVertex(finalX, finalY + height, 0.0);
        // Top-Left
        tess.setColorRGBA(topColor);
        tess.addVertex(finalX, finalY, 0.0);
        // Top-Right
        tess.setColorRGBA(topColor);
        tess.addVertex(finalX + width, finalY, 0.0);
        
        tess.draw();

        gl.ShadeModel(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Flat);
        gl.Disable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Blend);
        gl.Enable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.AlphaTest);
        gl.Enable(global::BetaSharp.Client.Rendering.Core.OpenGL.GLEnum.Texture2D);
    }
}
