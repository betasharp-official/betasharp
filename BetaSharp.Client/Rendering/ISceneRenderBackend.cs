namespace BetaSharp.Client.Rendering;

[Flags]
public enum SceneClearBufferMask
{
    None = 0,
    Color = 1 << 0,
    Depth = 1 << 1
}

public enum SceneMatrixMode
{
    Projection,
    Modelview,
    Texture
}

public enum SceneRenderCapability
{
    AlphaTest,
    Blend,
    ColorMaterial,
    CullFace,
    DepthTest,
    Fog,
    Lighting,
    PolygonOffsetFill,
    RescaleNormal,
    Texture2D
}

public enum SceneBlendFactor
{
    One,
    SrcColor,
    DstColor,
    SrcAlpha,
    OneMinusSrcAlpha
}

public enum SceneFogMode
{
    Exp,
    Linear
}

public enum SceneShadeModel
{
    Flat,
    Smooth
}

public enum SceneAlphaFunction
{
    Greater
}

public enum SceneColorMaterialFace
{
    Front
}

public enum SceneColorMaterialParameter
{
    Ambient
}

/// <summary>
/// Backend-facing scene render-state surface used by <see cref="GameRenderer"/>.
/// This keeps immediate scene orchestration independent from direct OpenGL calls.
/// </summary>
public interface ISceneRenderBackend
{
    int GenerateDisplayLists(int count);
    void BeginDisplayList(int listId);
    void EndDisplayList();
    void CallDisplayList(int listId);

    void Enable(SceneRenderCapability capability);
    void Disable(SceneRenderCapability capability);

    void SetMatrixMode(SceneMatrixMode matrixMode);
    void LoadIdentity();
    void PushMatrix();
    void PopMatrix();
    void Translate(float x, float y, float z);
    void Scale(float x, float y, float z);
    void Rotate(float angle, float x, float y, float z);
    void Perspective(float fieldOfView, float aspectRatio, float zNear, float zFar);
    void Ortho(double left, double right, double bottom, double top, double zNear, double zFar);

    void SetViewport(int x, int y, uint width, uint height);
    void Clear(SceneClearBufferMask clearMask);
    void ClearColor(float red, float green, float blue, float alpha);
    void SetDepthMask(bool enabled);

    void SetBlendFunction(SceneBlendFactor source, SceneBlendFactor destination);
    void SetAlphaFunction(SceneAlphaFunction function, float threshold);
    void SetShadeModel(SceneShadeModel shadeModel);
    void SetNormal(float x, float y, float z);
    void SetColorRgb(float red, float green, float blue);
    void SetColor(float red, float green, float blue, float alpha);
    void SetColorMask(bool red, bool green, bool blue, bool alpha);
    void SetLineWidth(float width);
    void SetPolygonOffset(float factor, float units);
    void SetColorMaterial(SceneColorMaterialFace face, SceneColorMaterialParameter parameter);

    void SetFogColor(float red, float green, float blue, float alpha);
    void SetFogMode(SceneFogMode fogMode);
    void SetFogDensity(float density);
    void SetFogStart(float start);
    void SetFogEnd(float end);
}
