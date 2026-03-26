using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;

namespace BetaSharp.Client.Guis;

public class Gui
{
    protected float _zLevel = 0.0F;

    protected static void DrawHorizontalLine(int startX, int endX, int y, Color color)
    {
        if (endX < startX) (startX, endX) = (endX, startX);
        DrawRect(startX, y, endX + 1, y + 1, color);
    }

    protected static void DrawVerticalLine(int x, int startY, int endY, Color color)
    {
        if (endY < startY) (startY, endY) = (endY, startY);
        DrawRect(x, startY + 1, x + 1, endY, color);
    }

    public static void DrawRect(int x1, int y1, int x2, int y2, Color color)
    {
        if (x1 < x2) (x1, x2) = (x2, x1);
        if (y1 < y2) (y1, y2) = (y2, y1);

        Tessellator tess = Tessellator.instance;

        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        tess.startDrawingQuads();
        tess.setColorRGBA(color);
        tess.addVertex(x1, y2, 0.0D);
        tess.addVertex(x2, y2, 0.0D);
        tess.addVertex(x2, y1, 0.0D);
        tess.addVertex(x1, y1, 0.0D);
        tess.draw();

        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Blend);
    }

    internal static void DrawGradientRect(int right, int bottom, int left, int top, Color topColor, Color bottomColor)
    {
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.ShadeModel(GLEnum.Smooth);

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.setColorRGBA(topColor);
        tess.addVertex(left, bottom, 0.0D);
        tess.addVertex(right, bottom, 0.0D);
        tess.setColorRGBA(bottomColor);
        tess.addVertex(right, top, 0.0D);
        tess.addVertex(left, top, 0.0D);
        tess.draw();

        GLManager.GL.ShadeModel(GLEnum.Flat);
        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    public static void DrawCenteredString(TextRenderer renderer, string text, int x, int y, Color color)
    {
        renderer.DrawStringWithShadow(text, x - renderer.GetStringWidth(text) / 2, y, color);
    }

    public static void DrawString(TextRenderer renderer, string text, int x, int y, Color color)
    {
        renderer.DrawStringWithShadow(text, x, y, color);
    }

    public void DrawTexturedModalRect(int x, int y, int u, int v, int width, int height)
    {
        float f = 0.00390625F;
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0, y + height, _zLevel, (double)((u + 0) * f), (double)((v + height) * f));
        tess.addVertexWithUV(x + width, y + height, _zLevel, (double)((u + width) * f), (double)((v + height) * f));
        tess.addVertexWithUV(x + width, y + 0, _zLevel, (double)((u + width) * f), (double)((v + 0) * f));
        tess.addVertexWithUV(x + 0, y + 0, _zLevel, (double)((u + 0) * f), (double)((v + 0) * f));
        tess.draw();
    }

    public void DrawTexturedModalRect(int x, int y, int u, int v, int width, int height, int textureWidth, int textureHeight)
    {
        float f = 1.0F / textureWidth;
        float f1 = 1.0F / textureHeight;
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0, y + height, _zLevel, (double)((u + 0) * f), (double)((v + height) * f1));
        tess.addVertexWithUV(x + width, y + height, _zLevel, (double)((u + width) * f), (double)((v + height) * f1));
        tess.addVertexWithUV(x + width, y + 0, _zLevel, (double)((u + width) * f), (double)((v + 0) * f1));
        tess.addVertexWithUV(x + 0, y + 0, _zLevel, (double)((u + 0) * f), (double)((v + 0) * f1));
        tess.draw();
    }
}
