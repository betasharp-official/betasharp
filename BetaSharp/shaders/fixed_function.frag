#version 450

layout (location = 0) flat in vec4 v_ColorFlat;
layout (location = 1) in vec4 v_ColorSmooth;
layout (location = 2) in vec2 v_TexCoord;
layout (location = 3) in float v_FogDist;

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

layout (set = 1, binding = 0) uniform sampler2D u_Texture0;

layout (location = 0) out vec4 FragColor;

void main()
{
    vec4 finalColor = u_ShadeModel == 1 ? v_ColorSmooth : v_ColorFlat;
    vec4 texColor = vec4(1.0);
    if (u_UseTexture != 0)
    {
        texColor = texture(u_Texture0, v_TexCoord);
    }
    FragColor = finalColor * texColor;

    if (FragColor.a < u_AlphaThreshold)
        discard;

    if (u_EnableFog != 0)
    {
        float fogFactor;
        if (u_FogMode == 0)
        {
            // Linear
            fogFactor = clamp((u_FogEnd - v_FogDist) / (u_FogEnd - u_FogStart), 0.0, 1.0);
        }
        else
        {
            // Exponential
            fogFactor = clamp(exp(-u_FogDensity * v_FogDist), 0.0, 1.0);
        }
        FragColor = mix(u_FogColor, FragColor, fogFactor);
    }
}
