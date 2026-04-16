using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpLegacyFixedFunctionApi : ILegacyFixedFunctionApi
{
    private int _nextDisplayListId = 1;

    public int GenerateDisplayLists(int count)
    {
        int id = _nextDisplayListId;
        _nextDisplayListId += Math.Max(1, count);
        return id;
    }

    public void BeginDisplayList(int listId)
    {
    }

    public void EndDisplayList()
    {
    }

    public void CallDisplayList(int listId)
    {
    }

    public void Enable(SceneRenderCapability capability)
    {
    }

    public void Disable(SceneRenderCapability capability)
    {
    }

    public void SetMatrixMode(SceneMatrixMode matrixMode)
    {
    }

    public void LoadIdentity()
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

    public void Perspective(float fieldOfView, float aspectRatio, float zNear, float zFar)
    {
    }

    public void Ortho(double left, double right, double bottom, double top, double zNear, double zFar)
    {
    }

    public void SetViewport(int x, int y, uint width, uint height)
    {
    }

    public void Clear(SceneClearBufferMask clearMask)
    {
    }

    public void ClearColor(float red, float green, float blue, float alpha)
    {
    }

    public void SetDepthMask(bool enabled)
    {
    }

    public void SetBlendFunction(SceneBlendFactor source, SceneBlendFactor destination)
    {
    }

    public void SetAlphaFunction(SceneAlphaFunction function, float threshold)
    {
    }

    public void SetShadeModel(SceneShadeModel shadeModel)
    {
    }

    public void SetNormal(float x, float y, float z)
    {
    }

    public void SetColorRgb(float red, float green, float blue)
    {
    }

    public void SetColor(float red, float green, float blue, float alpha)
    {
    }

    public void SetColorMask(bool red, bool green, bool blue, bool alpha)
    {
    }

    public void SetLineWidth(float width)
    {
    }

    public void SetPolygonOffset(float factor, float units)
    {
    }

    public void SetDepthFunction(SceneDepthFunction depthFunction)
    {
    }

    public void SetColorMaterial(SceneColorMaterialFace face, SceneColorMaterialParameter parameter)
    {
    }

    public void SetFogColor(float red, float green, float blue, float alpha)
    {
    }

    public void SetFogMode(SceneFogMode fogMode)
    {
    }

    public void SetFogDensity(float density)
    {
    }

    public void SetFogStart(float start)
    {
    }

    public void SetFogEnd(float end)
    {
    }
}
