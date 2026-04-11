using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class OpenGlSceneRenderBackend : ISceneRenderBackend
{
    public int GenerateDisplayLists(int count)
    {
        return GLAllocation.generateDisplayLists(count);
    }

    public void BeginDisplayList(int listId)
    {
        GLManager.GL.NewList((uint)listId, GLEnum.Compile);
    }

    public void EndDisplayList()
    {
        GLManager.GL.EndList();
    }

    public void CallDisplayList(int listId)
    {
        GLManager.GL.CallList((uint)listId);
    }

    public void Enable(SceneRenderCapability capability)
    {
        GLManager.GL.Enable(MapCapability(capability));
    }

    public void Disable(SceneRenderCapability capability)
    {
        GLManager.GL.Disable(MapCapability(capability));
    }

    public void SetMatrixMode(SceneMatrixMode matrixMode)
    {
        GLEnum glMatrixMode = matrixMode switch
        {
            SceneMatrixMode.Projection => GLEnum.Projection,
            SceneMatrixMode.Modelview => GLEnum.Modelview,
            SceneMatrixMode.Texture => GLEnum.Texture,
            _ => GLEnum.Modelview
        };
        GLManager.GL.MatrixMode(glMatrixMode);
    }

    public void LoadIdentity()
    {
        GLManager.GL.LoadIdentity();
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

    public void Perspective(float fieldOfView, float aspectRatio, float zNear, float zFar)
    {
        GLU.gluPerspective(fieldOfView, aspectRatio, zNear, zFar);
    }

    public void Ortho(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        GLManager.GL.Ortho(left, right, bottom, top, zNear, zFar);
    }

    public void SetViewport(int x, int y, uint width, uint height)
    {
        GLManager.GL.Viewport(x, y, width, height);
    }

    public void Clear(SceneClearBufferMask clearMask)
    {
        ClearBufferMask glMask = 0;
        if ((clearMask & SceneClearBufferMask.Color) != 0)
        {
            glMask |= ClearBufferMask.ColorBufferBit;
        }
        if ((clearMask & SceneClearBufferMask.Depth) != 0)
        {
            glMask |= ClearBufferMask.DepthBufferBit;
        }

        GLManager.GL.Clear(glMask);
    }

    public void ClearColor(float red, float green, float blue, float alpha)
    {
        GLManager.GL.ClearColor(red, green, blue, alpha);
    }

    public void SetDepthMask(bool enabled)
    {
        GLManager.GL.DepthMask(enabled);
    }

    public void SetBlendFunction(SceneBlendFactor source, SceneBlendFactor destination)
    {
        GLManager.GL.BlendFunc(MapBlendFactor(source), MapBlendFactor(destination));
    }

    public void SetAlphaFunction(SceneAlphaFunction function, float threshold)
    {
        GLManager.GL.AlphaFunc(MapAlphaFunction(function), threshold);
    }

    public void SetShadeModel(SceneShadeModel shadeModel)
    {
        GLManager.GL.ShadeModel(shadeModel == SceneShadeModel.Flat ? GLEnum.Flat : GLEnum.Smooth);
    }

    public void SetNormal(float x, float y, float z)
    {
        GLManager.GL.Normal3(x, y, z);
    }

    public void SetColorRgb(float red, float green, float blue)
    {
        GLManager.GL.Color3(red, green, blue);
    }

    public void SetColor(float red, float green, float blue, float alpha)
    {
        GLManager.GL.Color4(red, green, blue, alpha);
    }

    public void SetColorMask(bool red, bool green, bool blue, bool alpha)
    {
        GLManager.GL.ColorMask(red, green, blue, alpha);
    }

    public void SetLineWidth(float width)
    {
        GLManager.GL.LineWidth(width);
    }

    public void SetPolygonOffset(float factor, float units)
    {
        GLManager.GL.PolygonOffset(factor, units);
    }

    public void SetDepthFunction(SceneDepthFunction depthFunction)
    {
        GLManager.GL.DepthFunc(MapDepthFunction(depthFunction));
    }

    public void SetColorMaterial(SceneColorMaterialFace face, SceneColorMaterialParameter parameter)
    {
        GLManager.GL.ColorMaterial(MapColorMaterialFace(face), MapColorMaterialParameter(parameter));
    }

    public void SetFogColor(float red, float green, float blue, float alpha)
    {
        GLManager.GL.Fog(GLEnum.FogColor, [red, green, blue, alpha]);
    }

    public void SetFogMode(SceneFogMode fogMode)
    {
        GLManager.GL.Fog(GLEnum.FogMode, fogMode == SceneFogMode.Exp ? (int)GLEnum.Exp : (int)GLEnum.Linear);
    }

    public void SetFogDensity(float density)
    {
        GLManager.GL.Fog(GLEnum.FogDensity, density);
    }

    public void SetFogStart(float start)
    {
        GLManager.GL.Fog(GLEnum.FogStart, start);
    }

    public void SetFogEnd(float end)
    {
        GLManager.GL.Fog(GLEnum.FogEnd, end);
    }

    private static GLEnum MapCapability(SceneRenderCapability capability)
    {
        return capability switch
        {
            SceneRenderCapability.AlphaTest => GLEnum.AlphaTest,
            SceneRenderCapability.Blend => GLEnum.Blend,
            SceneRenderCapability.ColorMaterial => GLEnum.ColorMaterial,
            SceneRenderCapability.CullFace => GLEnum.CullFace,
            SceneRenderCapability.DepthTest => GLEnum.DepthTest,
            SceneRenderCapability.Fog => GLEnum.Fog,
            SceneRenderCapability.Lighting => GLEnum.Lighting,
            SceneRenderCapability.PolygonOffsetFill => GLEnum.PolygonOffsetFill,
            SceneRenderCapability.RescaleNormal => GLEnum.RescaleNormal,
            SceneRenderCapability.Texture2D => GLEnum.Texture2D,
            _ => GLEnum.Texture2D
        };
    }

    private static GLEnum MapBlendFactor(SceneBlendFactor factor)
    {
        return factor switch
        {
            SceneBlendFactor.SrcAlpha => GLEnum.SrcAlpha,
            SceneBlendFactor.One => GLEnum.One,
            SceneBlendFactor.SrcColor => GLEnum.SrcColor,
            SceneBlendFactor.DstColor => GLEnum.DstColor,
            SceneBlendFactor.DstAlpha => GLEnum.DstAlpha,
            SceneBlendFactor.OneMinusSrcAlpha => GLEnum.OneMinusSrcAlpha,
            _ => GLEnum.SrcAlpha
        };
    }

    private static GLEnum MapDepthFunction(SceneDepthFunction depthFunction)
    {
        return depthFunction switch
        {
            SceneDepthFunction.Equal => GLEnum.Equal,
            SceneDepthFunction.Lequal => GLEnum.Lequal,
            _ => GLEnum.Lequal
        };
    }

    private static GLEnum MapAlphaFunction(SceneAlphaFunction function)
    {
        return function switch
        {
            SceneAlphaFunction.Greater => GLEnum.Greater,
            _ => GLEnum.Greater
        };
    }

    private static GLEnum MapColorMaterialFace(SceneColorMaterialFace face)
    {
        return face switch
        {
            SceneColorMaterialFace.Front => GLEnum.Front,
            _ => GLEnum.Front
        };
    }

    private static GLEnum MapColorMaterialParameter(SceneColorMaterialParameter parameter)
    {
        return parameter switch
        {
            SceneColorMaterialParameter.Ambient => GLEnum.Ambient,
            _ => GLEnum.Ambient
        };
    }
}
