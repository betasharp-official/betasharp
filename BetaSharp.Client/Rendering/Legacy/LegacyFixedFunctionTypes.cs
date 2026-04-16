namespace BetaSharp.Client.Rendering.Legacy;

[Flags]
public enum SceneClearBufferMask
{
    Color = 1,
    Depth = 2,
    Stencil = 4
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
    Normalize,
    PolygonOffsetFill,
    RescaleNormal,
    Texture2D
}

public enum SceneMatrixMode
{
    Modelview,
    Projection,
    Texture
}

public enum SceneBlendFactor
{
    Zero,
    One,
    SrcColor,
    DstColor,
    DstAlpha,
    SrcAlpha,
    OneMinusSrcAlpha,
    OneMinusDstColor,
    OneMinusSrcColor
}

public enum SceneAlphaFunction
{
    Never,
    Less,
    Equal,
    Lequal,
    Greater,
    Notequal,
    Gequal,
    Always
}

public enum SceneShadeModel
{
    Flat,
    Smooth
}

public enum SceneDepthFunction
{
    Never,
    Less,
    Equal,
    Lequal,
    Greater,
    Notequal,
    Gequal,
    Always
}

public enum SceneColorMaterialFace
{
    Front,
    Back,
    FrontAndBack
}

public enum SceneColorMaterialParameter
{
    Ambient,
    Diffuse,
    Specular,
    Emission,
    AmbientAndDiffuse
}

public enum SceneFogMode
{
    Linear,
    Exp,
    Exp2
}
