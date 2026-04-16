using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Legacy;
using Silk.NET.OpenGL;
using LegacyGLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;
using static BetaSharp.Client.Rendering.Legacy.SceneClearBufferMask;
using static BetaSharp.Client.Rendering.Legacy.SceneRenderCapability;
using static BetaSharp.Client.Rendering.Legacy.SceneMatrixMode;
using static BetaSharp.Client.Rendering.Legacy.SceneBlendFactor;
using static BetaSharp.Client.Rendering.Legacy.SceneDepthFunction;
using static BetaSharp.Client.Rendering.Legacy.SceneFogMode;
using static BetaSharp.Client.Rendering.Legacy.SceneShadeModel;
using static BetaSharp.Client.Rendering.Legacy.SceneAlphaFunction;
using static BetaSharp.Client.Rendering.Legacy.SceneColorMaterialFace;
using static BetaSharp.Client.Rendering.Legacy.SceneColorMaterialParameter;

namespace BetaSharp.Client.Rendering.Backends.OpenGL;

internal sealed class OpenGlLegacyFixedFunctionApi : ILegacyFixedFunctionApi
{
    public int GenerateDisplayLists(int count) => GLAllocation.generateDisplayLists(count);
    public void BeginDisplayList(int listId) => GLManager.GL.NewList((uint)listId, LegacyGLEnum.Compile);
    public void EndDisplayList() => GLManager.GL.EndList();
    public void CallDisplayList(int listId) => GLManager.GL.CallList((uint)listId);
    public void Enable(SceneRenderCapability capability) => GLManager.GL.Enable(ToGl(capability));
    public void Disable(SceneRenderCapability capability) => GLManager.GL.Disable(ToGl(capability));
    public void SetMatrixMode(SceneMatrixMode matrixMode) => GLManager.GL.MatrixMode(ToGl(matrixMode));
    public void LoadIdentity() => GLManager.GL.LoadIdentity();
    public void PushMatrix() => GLManager.GL.PushMatrix();
    public void PopMatrix() => GLManager.GL.PopMatrix();
    public void Translate(float x, float y, float z) => GLManager.GL.Translate(x, y, z);
    public void Scale(float x, float y, float z) => GLManager.GL.Scale(x, y, z);
    public void Rotate(float angle, float x, float y, float z) => GLManager.GL.Rotate(angle, x, y, z);

    public void Perspective(float fieldOfView, float aspectRatio, float zNear, float zFar)
    {
        float fH = (float)Math.Tan(fieldOfView / 360.0 * Math.PI) * zNear;
        float fW = fH * aspectRatio;
        GLManager.GL.Frustum(-fW, fW, -fH, fH, zNear, zFar);
        float f = 1.0f / MathF.Tan(fieldOfView / 2.0f);
    }

    public void Ortho(double left, double right, double bottom, double top, double zNear, double zFar) =>
        GLManager.GL.Ortho(left, right, bottom, top, zNear, zFar);

    public void SetViewport(int x, int y, uint width, uint height) => GLManager.GL.Viewport(x, y, width, height);

    public void Clear(SceneClearBufferMask clearMask)
    {
        ClearBufferMask mask = 0;
        if ((clearMask & Color) != 0) mask |= ClearBufferMask.ColorBufferBit;
        if ((clearMask & Depth) != 0) mask |= ClearBufferMask.DepthBufferBit;
        GLManager.GL.Clear(mask);
    }

    public void ClearColor(float red, float green, float blue, float alpha) =>
        GLManager.GL.ClearColor(red, green, blue, alpha);

    public void SetDepthMask(bool enabled) => GLManager.GL.DepthMask(enabled);

    public void SetBlendFunction(SceneBlendFactor source, SceneBlendFactor destination) =>
        GLManager.GL.BlendFunc(ToGl(source), ToGl(destination));

    public void SetAlphaFunction(SceneAlphaFunction function, float threshold) =>
        GLManager.GL.AlphaFunc(ToGl(function), threshold);

    public void SetShadeModel(SceneShadeModel shadeModel) => GLManager.GL.ShadeModel(ToGl(shadeModel));
    public void SetNormal(float x, float y, float z) => GLManager.GL.Normal3(x, y, z);
    public void SetColorRgb(float red, float green, float blue) => GLManager.GL.Color3(red, green, blue);

    public void SetColor(float red, float green, float blue, float alpha) =>
        GLManager.GL.Color4(red, green, blue, alpha);

    public void SetColorMask(bool red, bool green, bool blue, bool alpha) =>
        GLManager.GL.ColorMask(red, green, blue, alpha);

    public void SetLineWidth(float width) => GLManager.GL.LineWidth(width);
    public void SetPolygonOffset(float factor, float units) => GLManager.GL.PolygonOffset(factor, units);
    public void SetDepthFunction(SceneDepthFunction depthFunction) => GLManager.GL.DepthFunc(ToGl(depthFunction));

    public void SetColorMaterial(SceneColorMaterialFace face, SceneColorMaterialParameter parameter) =>
        GLManager.GL.ColorMaterial(ToGl(face), ToGl(parameter));

    public void SetFogColor(float red, float green, float blue, float alpha) =>
        GLManager.GL.Fog(LegacyGLEnum.FogColor, [red, green, blue, alpha]);

    public void SetFogMode(SceneFogMode fogMode) => GLManager.GL.Fog(LegacyGLEnum.FogMode, (int)ToGl(fogMode));
    public void SetFogDensity(float density) => GLManager.GL.Fog(LegacyGLEnum.FogDensity, density);
    public void SetFogStart(float start) => GLManager.GL.Fog(LegacyGLEnum.FogStart, start);
    public void SetFogEnd(float end) => GLManager.GL.Fog(LegacyGLEnum.FogEnd, end);

    private static LegacyGLEnum ToGl(SceneRenderCapability value) => value switch
    {
        SceneRenderCapability.AlphaTest => LegacyGLEnum.AlphaTest,
        SceneRenderCapability.Blend => LegacyGLEnum.Blend,
        SceneRenderCapability.ColorMaterial => LegacyGLEnum.ColorMaterial,
        SceneRenderCapability.CullFace => LegacyGLEnum.CullFace,
        SceneRenderCapability.DepthTest => LegacyGLEnum.DepthTest,
        SceneRenderCapability.Fog => LegacyGLEnum.Fog,
        SceneRenderCapability.Lighting => LegacyGLEnum.Lighting,
        SceneRenderCapability.PolygonOffsetFill => LegacyGLEnum.PolygonOffsetFill,
        SceneRenderCapability.RescaleNormal => LegacyGLEnum.RescaleNormal,
        SceneRenderCapability.Texture2D => LegacyGLEnum.Texture2D,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneMatrixMode value) => value switch
    {
        SceneMatrixMode.Projection => LegacyGLEnum.Projection,
        SceneMatrixMode.Modelview => LegacyGLEnum.Modelview,
        SceneMatrixMode.Texture => LegacyGLEnum.Texture,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneBlendFactor value) => value switch
    {
        SceneBlendFactor.One => LegacyGLEnum.One,
        SceneBlendFactor.SrcColor => LegacyGLEnum.SrcColor,
        SceneBlendFactor.DstColor => LegacyGLEnum.DstColor,
        SceneBlendFactor.DstAlpha => LegacyGLEnum.DstAlpha,
        SceneBlendFactor.SrcAlpha => LegacyGLEnum.SrcAlpha,
        SceneBlendFactor.OneMinusSrcAlpha => LegacyGLEnum.OneMinusSrcAlpha,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneDepthFunction value) => value switch
    {
        SceneDepthFunction.Equal => LegacyGLEnum.Equal,
        SceneDepthFunction.Lequal => LegacyGLEnum.Lequal,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneFogMode value) => value switch
    {
        SceneFogMode.Exp => LegacyGLEnum.Exp,
        SceneFogMode.Linear => LegacyGLEnum.Linear,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneShadeModel value) => value switch
    {
        SceneShadeModel.Flat => LegacyGLEnum.Flat,
        SceneShadeModel.Smooth => LegacyGLEnum.Smooth,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneAlphaFunction value) => value switch
    {
        SceneAlphaFunction.Greater => LegacyGLEnum.Greater,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneColorMaterialFace value) => value switch
    {
        SceneColorMaterialFace.Front => LegacyGLEnum.Front,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static LegacyGLEnum ToGl(SceneColorMaterialParameter value) => value switch
    {
        SceneColorMaterialParameter.Ambient => LegacyGLEnum.Ambient,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
