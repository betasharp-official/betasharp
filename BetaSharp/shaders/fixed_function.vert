#version 450

layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec2 a_TexCoord;
layout (location = 2) in vec4 a_Color;
layout (location = 3) in vec3 a_Normal;

layout (set = 0, binding = 0) uniform Uniforms {
    mat4 u_ModelView;
    mat4 u_Projection;
    mat4 u_TextureMatrix;
    mat3 u_NormalMatrix;
    vec3 u_Light0Dir;
    float _pad0;
    vec3 u_Light0Diffuse;
    float _pad1;
    vec3 u_Light1Dir;
    float _pad2;
    vec3 u_Light1Diffuse;
    float _pad3;
    vec3 u_AmbientLight;
    int u_EnableLighting;
    float u_AlphaThreshold;
    int u_UseTexture;
    int u_ShadeModel;
    int u_EnableFog;
    int u_FogMode;
    float u_FogStart;
    float u_FogEnd;
    float u_FogDensity;
    vec4 u_FogColor;
};

layout (location = 0) flat out vec4 v_ColorFlat;
layout (location = 1) out vec4 v_ColorSmooth;
layout (location = 2) out vec2 v_TexCoord;
layout (location = 3) out float v_FogDist;

void main()
{
    vec4 tex = u_TextureMatrix * vec4(a_TexCoord, 0.0, 1.0);
    v_TexCoord = tex.xy;

    vec4 viewPos = u_ModelView * vec4(a_Position, 1.0);
    gl_Position = u_Projection * viewPos;
    v_FogDist = length(viewPos.xyz);

    vec4 finalColor;
    if (u_EnableLighting != 0)
    {
        vec3 normal = normalize(u_NormalMatrix * a_Normal);
        float diff0 = max(dot(normal, u_Light0Dir), 0.0);
        float diff1 = max(dot(normal, u_Light1Dir), 0.0);
        vec3 lighting = u_AmbientLight
                      + diff0 * u_Light0Diffuse
                      + diff1 * u_Light1Diffuse;
        finalColor = vec4(clamp(a_Color.rgb * lighting, 0.0, 1.0), a_Color.a);
    }
    else
    {
        finalColor = a_Color;
    }

    v_ColorFlat = finalColor;
    v_ColorSmooth = finalColor;
}
