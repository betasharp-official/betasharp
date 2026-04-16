using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.UI.Rendering;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Backends.OpenGL;

internal sealed class OpenGlUiRenderBackend : IUiRenderBackend
{
    public void BeginFrame()
    {
        GLManager.GL.UseProgram(0);
        GLManager.GL.ActiveTexture(GLEnum.Texture0);

        GLManager.GL.MatrixMode(GLEnum.Texture);
        GLManager.GL.LoadIdentity();
        GLManager.GL.MatrixMode(GLEnum.Modelview);

        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.Disable(GLEnum.CullFace);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1f, 1f, 1f, 1f);
        GLManager.GL.PushMatrix();
    }

    public void EndFrame()
    {
        GLManager.GL.PopMatrix();
        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
    }

    public void SetColor(Color color)
    {
        GLManager.GL.Color4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }

    public void ResetColor()
    {
        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
    }

    public void SetDepthMask(bool enabled)
    {
        GLManager.GL.DepthMask(enabled);
    }

    public void SetAlphaTest(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.AlphaTest);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.AlphaTest);
        }
    }

    public void SetBlendFunction(UiBlendFactor source, UiBlendFactor destination)
    {
        GLManager.GL.BlendFunc(MapBlend(source), MapBlend(destination));
    }

    public void ResetBlendFunction()
    {
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
    }

    public void ClearDepthBuffer()
    {
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    public void EnableScissor(int x, int y, uint width, uint height)
    {
        GLManager.GL.Enable(GLEnum.ScissorTest);
        GLManager.GL.Scissor(x, y, width, height);
    }

    public void DisableScissor()
    {
        GLManager.GL.Disable(GLEnum.ScissorTest);
    }

    public void PushMatrix()
    {
        GLManager.GL.PushMatrix();
    }

    public void PopMatrix()
    {
        GLManager.GL.PopMatrix();
    }

    public void Translate(float x, float y, float z)
    {
        GLManager.GL.Translate(x, y, z);
    }

    public void Scale(float x, float y, float z)
    {
        GLManager.GL.Scale(x, y, z);
    }

    public void Rotate(float angle, float x, float y, float z)
    {
        GLManager.GL.Rotate(angle, x, y, z);
    }

    public void SetLighting(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.Lighting);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.Lighting);
        }
    }

    public void SetDepthTest(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.DepthTest);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.DepthTest);
        }
    }

    public void SetCullFace(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.CullFace);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.CullFace);
        }
    }

    public void SetRescaleNormal(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.RescaleNormal);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.RescaleNormal);
        }
    }

    public void SetColorMaterial(bool enabled)
    {
        if (enabled)
        {
            GLManager.GL.Enable(GLEnum.ColorMaterial);
        }
        else
        {
            GLManager.GL.Disable(GLEnum.ColorMaterial);
        }
    }

    public void TurnOnLighting(bool mirrored = false)
    {
        Lighting.turnOn(mirrored);
    }

    public void TurnOnGuiLighting()
    {
        Lighting.turnOnGui();
    }

    public void TurnOffLighting()
    {
        Lighting.turnOff();
    }

    public void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop,
        double uRight, double vBottom)
    {
        DrawTexturedQuad(left, top, right, bottom, z, uLeft, vTop, uRight, vBottom, new Color(255, 255, 255, 255));
    }

    public void DrawTexturedQuad(float left, float top, float right, float bottom, float z, double uLeft, double vTop,
        double uRight, double vBottom, Color tint)
    {
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.setColorRGBA(tint);
        tessellator.addVertexWithUV(left, bottom, z, uLeft, vBottom);
        tessellator.addVertexWithUV(right, bottom, z, uRight, vBottom);
        tessellator.addVertexWithUV(right, top, z, uRight, vTop);
        tessellator.addVertexWithUV(left, top, z, uLeft, vTop);
        tessellator.draw();
    }

    public void DrawSolidQuad(int x1, int y1, int x2, int y2, Color color)
    {
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.setColorRGBA(color);
        tessellator.addVertex(x1, y2, 0.0D);
        tessellator.addVertex(x2, y2, 0.0D);
        tessellator.addVertex(x2, y1, 0.0D);
        tessellator.addVertex(x1, y1, 0.0D);
        tessellator.draw();

        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    public void DrawGradientQuad(int left, int top, int right, int bottom, Color topColor, Color bottomColor)
    {
        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.ShadeModel(GLEnum.Smooth);

        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.setColorRGBA(topColor);
        tessellator.addVertex(left, bottom, 0.0D);
        tessellator.addVertex(right, bottom, 0.0D);
        tessellator.setColorRGBA(bottomColor);
        tessellator.addVertex(right, top, 0.0D);
        tessellator.addVertex(left, top, 0.0D);
        tessellator.draw();

        GLManager.GL.ShadeModel(GLEnum.Flat);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    private static GLEnum MapBlend(UiBlendFactor factor)
    {
        return factor switch
        {
            UiBlendFactor.Zero => GLEnum.Zero,
            UiBlendFactor.One => GLEnum.One,
            UiBlendFactor.SrcAlpha => GLEnum.SrcAlpha,
            UiBlendFactor.OneMinusSrcAlpha => GLEnum.OneMinusSrcAlpha,
            UiBlendFactor.OneMinusDstColor => GLEnum.OneMinusDstColor,
            UiBlendFactor.OneMinusSrcColor => GLEnum.OneMinusSrcColor,
            _ => GLEnum.SrcAlpha
        };
    }
}
